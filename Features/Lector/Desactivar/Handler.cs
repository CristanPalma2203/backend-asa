using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lector.Desactivar;

public static class DesactivarLectorHandler
{
    public static async Task<Result> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var lector = await db.Lectores.FirstOrDefaultAsync(item => item.IdLector == id, cancellationToken);
        if (lector is null)
        {
            return Result.Failure(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        var tieneReservasActivas = await db.Reservas.AnyAsync(
            reserva => reserva.IdLector == id && reserva.Estado == EstadoReserva.Activa,
            cancellationToken);

        if (tieneReservasActivas)
        {
            return Result.Failure(new Error(
                "lector-con-reservas-activas",
                "El lector tiene reservas activas y no se puede desactivar.",
                ErrorType.BusinessRule));
        }

        lector.Estado = EstadoLector.Inactivo;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(item => item.IdLector == id, cancellationToken);
        if (usuario is not null)
        {
            usuario.Estado = EstadoUsuario.Inactivo;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
