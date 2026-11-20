using System.Security.Claims;
using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Reservas;

public sealed record CancelarReservaRequest(int Id);

public sealed class CancelarReservaValidator : AbstractValidator<CancelarReservaRequest>
{
    public CancelarReservaValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador de la reserva no es válido.");
    }
}

public static class CancelarReservaHandler
{
    public static async Task<Result> Handle(
        int id,
        ClaimsPrincipal usuario,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted,
            cancellationToken);

        var reserva = await db.Reservas
            .Include(item => item.Ejemplar)
            .FirstOrDefaultAsync(item => item.IdReserva == id, cancellationToken);

        if (reserva is null)
        {
            return Result.Failure(new Error(
                "reserva-no-encontrada",
                "No se encontró la reserva.",
                ErrorType.NotFound));
        }

        var acceso = Acceso.DenegarSiNoEsDuenoOAdmin(usuario, reserva.IdLector);
        if (acceso is not null)
        {
            return acceso;
        }

        if (reserva.Estado != EstadoReserva.Activa)
        {
            return Result.Failure(new Error(
                "reserva-no-activa",
                "Solo se puede cancelar una reserva activa.",
                ErrorType.BusinessRule));
        }

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        reserva.Estado = EstadoReserva.Cancelada;
        reserva.FechaCancelacion = ahora;
        reserva.Ejemplar.Estado = EstadoEjemplar.Disponible;
        reserva.Ejemplar.UpdatedAt = ahora;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CancelarReservaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/reservas")
            .WithTags("Reservas")
            .MapPost("/{id:int}/cancelar", async (
                [AsParameters] CancelarReservaRequest request,
                ClaimsPrincipal usuario,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await CancelarReservaHandler.Handle(request.Id, usuario, db, timeProvider, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CancelarReservaRequest>>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
