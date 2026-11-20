using backend_asa.Common.Results;
using backend_asa.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libro.Editar;

public static class EditarLibroHandler
{
    public static async Task<Result> Handle(
        int id,
        EditarLibroRequest request,
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

        var isbn = LibroIsbn.Limpiar(request.Isbn);
        var isbnOcupado = await db.Libros.AnyAsync(
            item => item.Isbn == isbn && item.IdLibro != id,
            cancellationToken);
        if (isbnOcupado)
        {
            return IsbnDuplicado();
        }

        libro.Isbn = isbn;
        libro.Titulo = request.Titulo.Trim();
        libro.Autor = request.Autor.Trim();
        libro.Categoria = request.Categoria.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsIsbnDuplicado(exception))
        {
            return IsbnDuplicado();
        }

        return Result.Success();
    }

    private static Result IsbnDuplicado() =>
        Result.Failure(new Error(
            "isbn-duplicado",
            "Ya existe un libro con ese ISBN.",
            ErrorType.Conflict));

    private static bool EsIsbnDuplicado(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}
