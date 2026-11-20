using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplares;

public sealed record ListarEjemplaresRequest(int IdLibro);

public sealed record ListarEjemplaresItem(int IdEjemplar, string CodigoInventario, EstadoEjemplar Estado, string RowVersion);

public sealed class ListarEjemplaresValidator : AbstractValidator<ListarEjemplaresRequest>
{
    public ListarEjemplaresValidator()
    {
        RuleFor(x => x.IdLibro)
            .GreaterThan(0)
            .WithMessage("El identificador del libro no es válido.");
    }
}

public static class ListarEjemplaresHandler
{
    public static async Task<Result<IReadOnlyList<ListarEjemplaresItem>>> Handle(
        int idLibro,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var libroExiste = await db.Libros
            .AsNoTracking()
            .AnyAsync(libro => libro.IdLibro == idLibro, cancellationToken);

        if (!libroExiste)
        {
            return Result.Failure<IReadOnlyList<ListarEjemplaresItem>>(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        var filas = await db.Ejemplares
            .AsNoTracking()
            .Where(ejemplar => ejemplar.IdLibro == idLibro)
            .OrderBy(ejemplar => ejemplar.CodigoInventario)
            .Select(ejemplar => new
            {
                ejemplar.IdEjemplar,
                ejemplar.CodigoInventario,
                ejemplar.Estado,
                ejemplar.RowVersion
            })
            .ToListAsync(cancellationToken);

        IReadOnlyList<ListarEjemplaresItem> items = filas
            .Select(ejemplar => new ListarEjemplaresItem(
                ejemplar.IdEjemplar,
                ejemplar.CodigoInventario,
                ejemplar.Estado,
                Convert.ToBase64String(ejemplar.RowVersion)))
            .ToList();

        return Result.Success(items);
    }
}

public sealed class ListarEjemplaresEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros/{idLibro:int}/ejemplares")
            .WithTags("Ejemplares")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .MapGet("", async (
                [AsParameters] ListarEjemplaresRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ListarEjemplaresHandler.Handle(request.IdLibro, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ListarEjemplaresRequest>>()
            .Produces<IReadOnlyList<ListarEjemplaresItem>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
