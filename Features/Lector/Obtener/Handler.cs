using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lector.Obtener;

public static class ObtenerLectorHandler
{
    public static async Task<Result<ObtenerLectorResponse>> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var lector = await db.Lectores
            .AsNoTracking()
            .Where(item => item.IdLector == id)
            .Select(item => new ObtenerLectorResponse(
                item.IdLector,
                item.Identificador,
                item.Nombre,
                item.Correo,
                item.Estado,
                db.Usuarios.Any(usuario => usuario.IdLector == item.IdLector),
                item.Reservas.Count(reserva => reserva.Estado == EstadoReserva.Activa)))
            .FirstOrDefaultAsync(cancellationToken);

        if (lector is null)
        {
            return Result.Failure<ObtenerLectorResponse>(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        return Result.Success(lector);
    }
}
