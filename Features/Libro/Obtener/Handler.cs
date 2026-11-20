using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libro.Obtener;

public static class ObtenerLibroHandler
{
    public static async Task<Result<ObtenerLibroResponse>> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var libro = await db.Libros
            .AsNoTracking()
            .Where(item => item.IdLibro == id)
            .Select(item => new
            {
                item.IdLibro,
                item.Isbn,
                item.Titulo,
                item.Autor,
                item.Categoria,
                item.Estado,
                Disponibles = item.Ejemplares.Count(ejemplar => ejemplar.Estado == EstadoEjemplar.Disponible),
                Ejemplares = item.Ejemplares
                    .OrderBy(ejemplar => ejemplar.CodigoInventario)
                    .Select(ejemplar => new
                    {
                        ejemplar.IdEjemplar,
                        ejemplar.CodigoInventario,
                        ejemplar.Estado,
                        ejemplar.RowVersion
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (libro is null)
        {
            return Result.Failure<ObtenerLibroResponse>(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        return Result.Success(new ObtenerLibroResponse(
            libro.IdLibro,
            libro.Isbn,
            libro.Titulo,
            libro.Autor,
            libro.Categoria,
            libro.Estado,
            libro.Disponibles,
            libro.Ejemplares
                .Select(ejemplar => new ObtenerLibroEjemplar(
                    ejemplar.IdEjemplar,
                    ejemplar.CodigoInventario,
                    ejemplar.Estado,
                    Convert.ToBase64String(ejemplar.RowVersion)))
                .ToList()));
    }
}
