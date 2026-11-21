using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplar.Desactivar;

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

        ejemplar.Estado = EstadoEjemplar.Inactivo;
        ejemplar.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
