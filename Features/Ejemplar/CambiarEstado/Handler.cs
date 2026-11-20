using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplar.CambiarEstado;

public static class CambiarEstadoEjemplarHandler
{
    public static async Task<Result> Handle(
        int idLibro,
        int idEjemplar,
        CambiarEstadoEjemplarRequest request,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (request.Estado is not (EstadoEjemplar.Disponible or EstadoEjemplar.Inactivo))
        {
            return Result.Failure(new Error(
                "estado-no-permitido",
                "Desde aquí solo se puede pasar el ejemplar a Disponible o Inactivo.",
                ErrorType.BusinessRule));
        }

        var ejemplar = await db.Ejemplares.FirstOrDefaultAsync(
            item => item.IdEjemplar == idEjemplar && item.IdLibro == idLibro,
            cancellationToken);

        if (ejemplar is null)
        {
            return Result.Failure(new Error(
                "ejemplar-no-encontrado",
                "No se encontró el ejemplar.",
                ErrorType.NotFound));
        }

        var reservado = ejemplar.Estado == EstadoEjemplar.Reservado
            || await db.Reservas.AnyAsync(
                reserva => reserva.IdEjemplar == idEjemplar && reserva.Estado == EstadoReserva.Activa,
                cancellationToken);

        if (reservado)
        {
            return Result.Failure(new Error(
                "ejemplar-reservado",
                "El ejemplar tiene una reserva activa y no se puede cambiar su estado.",
                ErrorType.BusinessRule));
        }

        if (ejemplar.Estado is not (EstadoEjemplar.Disponible or EstadoEjemplar.Inactivo))
        {
            return Result.Failure(new Error(
                "estado-no-permitido",
                "Desde aquí solo se puede cambiar un ejemplar Disponible o Inactivo.",
                ErrorType.BusinessRule));
        }

        RowVersionToken.TryRead(request.RowVersion, out var rowVersion);
        db.Entry(ejemplar).Property(item => item.RowVersion).OriginalValue = rowVersion;
        ejemplar.Estado = request.Estado;
        ejemplar.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
