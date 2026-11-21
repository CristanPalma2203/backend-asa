using System.Security.Claims;
using backend_asa.Common.Results;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Reserva.Obtener;

public static class ObtenerReservaHandler
{
    public static async Task<Result<ReservaDetalle>> Handle(
        int id,
        ClaimsPrincipal usuario,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var fila = await db.Reservas
            .AsNoTracking()
            .Where(item => item.IdReserva == id)
            .Select(item => new
            {
                item.IdLector,
                Detalle = new ReservaDetalle(
                    item.IdReserva,
                    item.Ejemplar.IdLibro,
                    item.Ejemplar.Libro.Titulo,
                    item.Ejemplar.CodigoInventario,
                    item.Estado,
                    item.FechaSolicitud,
                    item.FechaCancelacion)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (fila is null)
        {
            return Result.Failure<ReservaDetalle>(new Error(
                "reserva-no-encontrada",
                "No se encontró la reserva.",
                ErrorType.NotFound));
        }

        if (Acceso.DenegarSiNoEsDuenoOAdmin(usuario, fila.IdLector) is { } accesoDenegado)
        {
            return Result.Failure<ReservaDetalle>(accesoDenegado);
        }

        return Result.Success(fila.Detalle);
    }
}
