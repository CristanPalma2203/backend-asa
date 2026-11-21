using backend_asa.Common.Results;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplar.Listar;

public static class ListarEjemplaresHandler
{
    public static async Task<Result<IReadOnlyList<ListarEjemplaresItem>>> Handle(
        int idLibro,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var libroExiste = await db.Libros
            .AsNoTracking()
            .AnyAsync(libro => libro.IdLibro == idLibro, cancellationToken);

        if (!libroExiste)
        {
            return Result.Failure<IReadOnlyList<ListarEjemplaresItem>>(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        var filas = await db.Ejemplares
            .AsNoTracking()
            .Where(ejemplar => ejemplar.IdLibro == idLibro)
            .OrderBy(ejemplar => ejemplar.CodigoInventario)
            .Select(ejemplar => new
            {
                ejemplar.IdEjemplar,
                ejemplar.CodigoInventario,
                ejemplar.Estado,
                ejemplar.RowVersion
            })
            .ToListAsync(cancellationToken);

        IReadOnlyList<ListarEjemplaresItem> items = filas
            .Select(ejemplar => new ListarEjemplaresItem(
                ejemplar.IdEjemplar,
                ejemplar.CodigoInventario,
                ejemplar.Estado,
                Convert.ToBase64String(ejemplar.RowVersion)))
            .ToList();

        return Result.Success(items);
    }
}
