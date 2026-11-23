using backend_asa.Common.Pagination;
using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libro.Listar;

public static class ListarLibrosHandler
{
    private const string SinMayusculasNiAcentos = "Latin1_General_CI_AI";

    public static async Task<Result<PagedResponse<ListarLibrosItem>>> Handle(
        ListarLibrosRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Libros
            .AsNoTracking()
            .Where(libro => libro.Estado == EstadoLibro.Activo);

        if (!string.IsNullOrWhiteSpace(request.TextoBuscado))
        {
            var buscar = request.TextoBuscado.Trim();
            query = query.Where(libro =>
                EF.Functions.Collate(libro.Titulo, SinMayusculasNiAcentos).Contains(buscar) ||
                EF.Functions.Collate(libro.Autor, SinMayusculasNiAcentos).Contains(buscar) ||
                EF.Functions.Collate(libro.Isbn, SinMayusculasNiAcentos).Contains(buscar) ||
                EF.Functions.Collate(libro.Categoria, SinMayusculasNiAcentos).Contains(buscar));
        }

        if (!string.IsNullOrWhiteSpace(request.Autor))
        {
            var autor = request.Autor.Trim().ToLowerInvariant();
            query = query.Where(libro => libro.Autor.ToLower().Contains(autor));
        }

        if (!string.IsNullOrWhiteSpace(request.Categoria))
        {
            var categoria = request.Categoria.Trim();
            query = query.Where(libro => libro.Categoria == categoria);
        }

        if (!string.IsNullOrWhiteSpace(request.CategoriaId))
        {
            var categoriaId = request.CategoriaId.Trim();
            query = query.Where(libro => libro.Categoria == categoriaId);
        }

        var proyectada = query
            .OrderBy(libro => libro.Titulo)
            .Select(libro => new ListarLibrosItem(
                libro.IdLibro,
                libro.Isbn,
                libro.Titulo,
                libro.Autor,
                libro.Categoria,
                libro.Ejemplares.Count(ejemplar => ejemplar.Estado != EstadoEjemplar.Inactivo),
                libro.Ejemplares.Count(ejemplar => ejemplar.Estado == EstadoEjemplar.Disponible)));

        var pagina = await proyectada.ToPagedResponseAsync(
            new PagedRequest(request.Page, request.PageSize),
            cancellationToken);

        return Result.Success(pagina);
    }
}
