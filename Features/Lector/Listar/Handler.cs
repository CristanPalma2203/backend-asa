using backend_asa.Common.Pagination;
using backend_asa.Common.Results;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lector.Listar;

public static class ListarLectoresHandler
{
    public static async Task<Result<PagedResponse<ListarLectoresItem>>> Handle(
        ListarLectoresRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Lectores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Buscar))
        {
            var buscar = request.Buscar.Trim();
            query = query.Where(lector =>
                lector.Nombre.Contains(buscar) ||
                lector.Correo.Contains(buscar) ||
                lector.Identificador.Contains(buscar));
        }

        if (request.Estado is not null)
        {
            query = query.Where(lector => lector.Estado == request.Estado);
        }

        var proyectada = query
            .OrderBy(lector => lector.Nombre)
            .Select(lector => new ListarLectoresItem(
                lector.IdLector,
                lector.Identificador,
                lector.Nombre,
                lector.Correo,
                lector.Estado,
                db.Usuarios.Any(usuario => usuario.IdLector == lector.IdLector)));

        var pagina = await proyectada.ToPagedResponseAsync(
            new PagedRequest(request.Page, request.PageSize),
            cancellationToken);

        return Result.Success(pagina);
    }
}
