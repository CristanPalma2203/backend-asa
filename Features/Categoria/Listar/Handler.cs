using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Categoria.Listar;

public static class ListarCategoriasHandler
{
    public static async Task<Result<IReadOnlyList<CategoriaItem>>> Handle(
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var categorias = await db.Libros
            .AsNoTracking()
            .Where(libro => libro.Estado == EstadoLibro.Activo)
            .GroupBy(libro => libro.Categoria)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new CategoriaItem(grupo.Key, grupo.Key, grupo.Count()))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CategoriaItem>>(categorias);
    }
}
