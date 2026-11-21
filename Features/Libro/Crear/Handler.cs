using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using backend_asa.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libro.Crear;

public static class CrearLibroHandler
{
    public static async Task<Result<CrearLibroResponse>> Handle(
        CrearLibroRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var isbn = LibroIsbn.Limpiar(request.Isbn);

        var libro = new Domain.Libro
        {
            Isbn = isbn,
            Titulo = request.Titulo.Trim(),
            Autor = request.Autor.Trim(),
            Categoria = request.Categoria.Trim(),
            Estado = EstadoLibro.Activo
        };

        db.Libros.Add(libro);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.EsViolacionDeIndiceUnico())
        {
            return IsbnDuplicado();
        }

        return Result.Success(new CrearLibroResponse(
            libro.IdLibro,
            libro.Isbn,
            libro.Titulo,
            libro.Autor,
            libro.Categoria));
    }

    private static Result<CrearLibroResponse> IsbnDuplicado() =>
        Result.Failure<CrearLibroResponse>(new Error(
            "isbn-duplicado",
            "Ya existe un libro con ese ISBN.",
            ErrorType.Conflict));
}
