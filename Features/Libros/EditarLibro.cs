using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libros;

public sealed record EditarLibroRequest(string Isbn, string Titulo, string Autor, string Categoria);

public sealed class EditarLibroValidator : AbstractValidator<EditarLibroRequest>
{
    public EditarLibroValidator()
    {
        RuleFor(x => x.Isbn)
            .NotEmpty().WithMessage("El ISBN es obligatorio.")
            .Must(LibroIsbn.EsValido).WithMessage("El ISBN debe ser ISBN-10 o ISBN-13 con dígito verificador válido.");

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(200).WithMessage("El título no puede superar 200 caracteres.");

        RuleFor(x => x.Autor)
            .NotEmpty().WithMessage("El autor es obligatorio.")
            .MaximumLength(150).WithMessage("El autor no puede superar 150 caracteres.");

        RuleFor(x => x.Categoria)
            .NotEmpty().WithMessage("La categoría es obligatoria.")
            .MaximumLength(80).WithMessage("La categoría no puede superar 80 caracteres.");
    }
}

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

public sealed class EditarLibroEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros")
            .WithTags("Libros")
            .MapPut("/{id:int}", async (
                int id,
                EditarLibroRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await EditarLibroHandler.Handle(id, request, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<EditarLibroRequest>>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
