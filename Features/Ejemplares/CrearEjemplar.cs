using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplares;

public sealed record CrearEjemplarRequest(string CodigoInventario);

public sealed record CrearEjemplarResponse(int IdEjemplar, string CodigoInventario, EstadoEjemplar Estado, string RowVersion);

public sealed class CrearEjemplarValidator : AbstractValidator<CrearEjemplarRequest>
{
    public const int CodigoMaxLength = 30;

    public CrearEjemplarValidator()
    {
        RuleFor(x => CodigoInventario.Normalizar(x.CodigoInventario))
            .NotEmpty().WithMessage("El código de inventario es obligatorio.")
            .MaximumLength(CodigoMaxLength).WithMessage("El código de inventario no puede superar 30 caracteres.")
            .OverridePropertyName(nameof(CrearEjemplarRequest.CodigoInventario));
    }
}

internal static class CodigoInventario
{
    public static string Normalizar(string? codigo) =>
        string.IsNullOrWhiteSpace(codigo)
            ? string.Empty
            : string.Concat(codigo.Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();
}

public static class CrearEjemplarHandler
{
    public static async Task<Result<CrearEjemplarResponse>> Handle(
        int idLibro,
        CrearEjemplarRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var libro = await db.Libros
            .AsNoTracking()
            .Where(item => item.IdLibro == idLibro)
            .Select(item => new { item.Estado })
            .FirstOrDefaultAsync(cancellationToken);

        if (libro is null)
        {
            return Result.Failure<CrearEjemplarResponse>(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        if (libro.Estado == EstadoLibro.Inactivo)
        {
            return Result.Failure<CrearEjemplarResponse>(new Error(
                "libro-inactivo",
                "El libro está inactivo y no admite ejemplares.",
                ErrorType.BusinessRule));
        }

        var codigo = CodigoInventario.Normalizar(request.CodigoInventario);
        var codigoExiste = await db.Ejemplares.AnyAsync(
            ejemplar => ejemplar.CodigoInventario == codigo,
            cancellationToken);

        if (codigoExiste)
        {
            return CodigoDuplicado();
        }

        var ahora = DateTime.UtcNow;
        var ejemplar = new Ejemplar
        {
            IdLibro = idLibro,
            CodigoInventario = codigo,
            Estado = EstadoEjemplar.Disponible,
            CreatedAt = ahora,
            UpdatedAt = ahora
        };

        db.Ejemplares.Add(ejemplar);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsCodigoDuplicado(exception))
        {
            return CodigoDuplicado();
        }

        return Result.Success(new CrearEjemplarResponse(
            ejemplar.IdEjemplar,
            ejemplar.CodigoInventario,
            ejemplar.Estado,
            Convert.ToBase64String(ejemplar.RowVersion)));
    }

    private static Result<CrearEjemplarResponse> CodigoDuplicado() =>
        Result.Failure<CrearEjemplarResponse>(new Error(
            "codigo-inventario-duplicado",
            "Ya existe un ejemplar con ese código de inventario.",
            ErrorType.Conflict));

    private static bool EsCodigoDuplicado(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}

public sealed class CrearEjemplarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros/{idLibro:int}/ejemplares")
            .WithTags("Ejemplares")
            .MapPost("", async (
                int idLibro,
                CrearEjemplarRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await CrearEjemplarHandler.Handle(idLibro, request, db, cancellationToken);
                return result.IsSuccess
                    ? result.ToHttpResult($"/api/libros/{idLibro}/ejemplares/{result.Value!.IdEjemplar}")
                    : result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CrearEjemplarRequest>>()
            .Produces<CrearEjemplarResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
