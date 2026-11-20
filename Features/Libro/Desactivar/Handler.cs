using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libro.Desactivar;

public static class DesactivarLibroHandler
{
    public static async Task<Result> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var libro = await db.Libros.FirstOrDefaultAsync(item => item.IdLibro == id, cancellationToken);
        if (libro is null)
        {
            return Result.Failure(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        var tieneReservasActivas = await db.Reservas.AnyAsync(
            reserva => reserva.Estado == EstadoReserva.Activa && reserva.Ejemplar.IdLibro == id,
            cancellationToken);

        if (tieneReservasActivas)
        {
            return Result.Failure(new Error(
                "libro-con-reservas-activas",
                "El libro tiene reservas activas y no se puede desactivar.",
                ErrorType.BusinessRule));
        }

        libro.Estado = EstadoLibro.Inactivo;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
