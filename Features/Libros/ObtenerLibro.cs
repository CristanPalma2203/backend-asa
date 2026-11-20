using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libros;

public sealed record ObtenerLibroRequest(int Id);

public sealed record ObtenerLibroEjemplar(int IdEjemplar, string CodigoInventario, EstadoEjemplar Estado, string RowVersion);

public sealed record ObtenerLibroResponse(
    int IdLibro,
    string Isbn,
    string Titulo,
    string Autor,
    string Categoria,
    EstadoLibro Estado,
    int Disponibles,
    IReadOnlyList<ObtenerLibroEjemplar> Ejemplares);

public sealed class ObtenerLibroValidator : AbstractValidator<ObtenerLibroRequest>
{
    public ObtenerLibroValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del libro no es válido.");
    }
}

public static class ObtenerLibroHandler
{
    public static async Task<Result<ObtenerLibroResponse>> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var libro = await db.Libros
            .AsNoTracking()
            .Where(item => item.IdLibro == id)
            .Select(item => new
            {
                item.IdLibro,
                item.Isbn,
                item.Titulo,
                item.Autor,
                item.Categoria,
                item.Estado,
                Disponibles = item.Ejemplares.Count(ejemplar => ejemplar.Estado == EstadoEjemplar.Disponible),
                Ejemplares = item.Ejemplares
                    .OrderBy(ejemplar => ejemplar.CodigoInventario)
                    .Select(ejemplar => new
                    {
                        ejemplar.IdEjemplar,
                        ejemplar.CodigoInventario,
                        ejemplar.Estado,
                        ejemplar.RowVersion
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (libro is null)
        {
            return Result.Failure<ObtenerLibroResponse>(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        return Result.Success(new ObtenerLibroResponse(
            libro.IdLibro,
            libro.Isbn,
            libro.Titulo,
            libro.Autor,
            libro.Categoria,
            libro.Estado,
            libro.Disponibles,
            libro.Ejemplares
                .Select(ejemplar => new ObtenerLibroEjemplar(
                    ejemplar.IdEjemplar,
                    ejemplar.CodigoInventario,
                    ejemplar.Estado,
                    Convert.ToBase64String(ejemplar.RowVersion)))
                .ToList()));
    }
}

public sealed class ObtenerLibroEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros")
            .WithTags("Libros")
            .MapGet("/{id:int}", async (
                [AsParameters] ObtenerLibroRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ObtenerLibroHandler.Handle(request.Id, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ObtenerLibroRequest>>()
            .AllowAnonymous()
            .Produces<ObtenerLibroResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
