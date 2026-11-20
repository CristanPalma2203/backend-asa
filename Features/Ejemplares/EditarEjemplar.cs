using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplares;

public sealed record EditarEjemplarRequest(string CodigoInventario, string RowVersion);

public sealed class EditarEjemplarValidator : AbstractValidator<EditarEjemplarRequest>
{
    public EditarEjemplarValidator()
    {
        RuleFor(x => CodigoInventario.Normalizar(x.CodigoInventario))
            .NotEmpty().WithMessage("El código de inventario es obligatorio.")
            .MaximumLength(CrearEjemplarValidator.CodigoMaxLength).WithMessage("El código de inventario no puede superar 30 caracteres.")
            .OverridePropertyName(nameof(EditarEjemplarRequest.CodigoInventario));

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("La versión del registro es obligatoria.")
            .Must(RowVersionToken.EsValido)
            .WithMessage("La versión del registro no es válida.");
    }
}

public static class EditarEjemplarHandler
{
    public static async Task<Result> Handle(
        int id,
        EditarEjemplarRequest request,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var ejemplar = await db.Ejemplares.FirstOrDefaultAsync(item => item.IdEjemplar == id, cancellationToken);
        if (ejemplar is null)
        {
            return Result.Failure(new Error(
                "ejemplar-no-encontrado",
                "No se encontró el ejemplar.",
                ErrorType.NotFound));
        }

        var codigo = CodigoInventario.Normalizar(request.CodigoInventario);
        var codigoOcupado = await db.Ejemplares.AnyAsync(
            item => item.CodigoInventario == codigo && item.IdEjemplar != id,
            cancellationToken);
        if (codigoOcupado)
        {
            return CodigoDuplicado();
        }

        // Si otro usuario cambió el ejemplar después de que el cliente lo leyó, EF lanza
        // DbUpdateConcurrencyException y GlobalExceptionHandler responde 409 conflicto-concurrencia.
        RowVersionToken.TryRead(request.RowVersion, out var rowVersion);
        db.Entry(ejemplar).Property(item => item.RowVersion).OriginalValue = rowVersion;
        ejemplar.CodigoInventario = codigo;
        ejemplar.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsCodigoDuplicado(exception))
        {
            return CodigoDuplicado();
        }

        return Result.Success();
    }

    private static Result CodigoDuplicado() =>
        Result.Failure(new Error(
            "codigo-inventario-duplicado",
            "Ya existe un ejemplar con ese código de inventario.",
            ErrorType.Conflict));

    private static bool EsCodigoDuplicado(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}

public sealed class EditarEjemplarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/ejemplares")
            .WithTags("Ejemplares")
            .MapPut("/{id:int}", async (
                int id,
                EditarEjemplarRequest request,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await EditarEjemplarHandler.Handle(id, request, db, timeProvider, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<EditarEjemplarRequest>>()
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
