using System.Security.Claims;
using backend_asa.Common.Pagination;
using backend_asa.Common.Results;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Reserva.ListarPorLector;

public static class ListarReservasPorLectorHandler
{
    public static async Task<Result<PagedResponse<ReservaDetalle>>> Handle(
        ListarReservasPorLectorRequest request,
        ClaimsPrincipal usuario,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        if (Acceso.DenegarSiNoEsDuenoOAdmin(usuario, request.LectorId) is { } accesoDenegado)
        {
            return Result.Failure<PagedResponse<ReservaDetalle>>(accesoDenegado);
        }

        var lectorExiste = await db.Lectores
            .AsNoTracking()
            .AnyAsync(lector => lector.IdLector == request.LectorId, cancellationToken);

        if (!lectorExiste)
        {
            return Result.Failure<PagedResponse<ReservaDetalle>>(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        var query = db.Reservas
            .AsNoTracking()
            .Where(reserva => reserva.IdLector == request.LectorId);

        if (request.Estado is not null)
        {
            query = query.Where(reserva => reserva.Estado == request.Estado);
        }

        var proyectada = query
            .OrderByDescending(reserva => reserva.FechaSolicitud)
            .Select(reserva => new ReservaDetalle(
                reserva.IdReserva,
                reserva.Ejemplar.IdLibro,
                reserva.Ejemplar.Libro.Titulo,
                reserva.Ejemplar.CodigoInventario,
                reserva.Estado,
                reserva.FechaSolicitud,
                reserva.FechaCancelacion));

        var pagina = await proyectada.ToPagedResponseAsync(
            new PagedRequest(request.Page, request.PageSize),
            cancellationToken);

        return Result.Success(pagina);
    }
}
