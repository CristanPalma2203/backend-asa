using System.Security.Claims;
using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Reserva.Cancelar;

public static class CancelarReservaHandler
{
    public static async Task<Result> Handle(
        int id,
        ClaimsPrincipal usuario,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var estrategiaReintentos = db.Database.CreateExecutionStrategy();
        return await estrategiaReintentos.ExecuteAsync(
            async ct =>
            {
                db.ChangeTracker.Clear();
                return await EjecutarIntentoAsync(id, usuario, db, timeProvider, ct);
            },
            cancellationToken);
    }

    private static async Task<Result> EjecutarIntentoAsync(
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
            .FromSql($"SELECT * FROM Reservas WITH (UPDLOCK, ROWLOCK) WHERE IdReserva = {id}")
            .Include(item => item.Ejemplar)
            .FirstOrDefaultAsync(cancellationToken);

        if (reserva is null)
        {
            return Result.Failure(new Error(
                "reserva-no-encontrada",
                "No se encontró la reserva.",
                ErrorType.NotFound));
        }

        if (Acceso.DenegarSiNoEsDuenoOAdmin(usuario, reserva.IdLector) is { } accesoDenegado)
        {
            return Result.Failure(accesoDenegado);
        }

        if (reserva.Estado != EstadoReserva.Activa)
        {
            return Result.Failure(new Error(
                "reserva-no-activa",
                "Solo se puede cancelar una reserva activa.",
                ErrorType.Conflict));
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
