using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplares;

public sealed record DesactivarEjemplarRequest(int Id);

public sealed class DesactivarEjemplarValidator : AbstractValidator<DesactivarEjemplarRequest>
{
    public DesactivarEjemplarValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del ejemplar no es válido.");
    }
}

public static class DesactivarEjemplarHandler
{
    public static async Task<Result> Handle(
        int id,
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

        var reservado = ejemplar.Estado == EstadoEjemplar.Reservado
            || await db.Reservas.AnyAsync(
                reserva => reserva.IdEjemplar == id && reserva.Estado == EstadoReserva.Activa,
                cancellationToken);

        if (reservado)
        {
            return Result.Failure(new Error(
                "ejemplar-reservado",
                "El ejemplar tiene una reserva activa y no se puede desactivar.",
                ErrorType.BusinessRule));
        }

        if (ejemplar.Estado == EstadoEjemplar.Inactivo)
        {
            return Result.Success();
        }

        // Baja lógica: la fila se conserva para no perder el historial de reservas.
        // Si una reserva toma la copia entre la lectura y el guardado, la rowversion ya no
        // coincide y GlobalExceptionHandler responde 409 conflicto-concurrencia.
        ejemplar.Estado = EstadoEjemplar.Inactivo;
        ejemplar.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class DesactivarEjemplarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/ejemplares")
            .WithTags("Ejemplares")
            .MapDelete("/{id:int}", async (
                [AsParameters] DesactivarEjemplarRequest request,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await DesactivarEjemplarHandler.Handle(request.Id, db, timeProvider, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<DesactivarEjemplarRequest>>()
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
