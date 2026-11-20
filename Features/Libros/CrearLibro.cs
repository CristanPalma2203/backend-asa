using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libros;

public sealed record CrearLibroRequest(string Isbn, string Titulo, string Autor, string Categoria);

public sealed record CrearLibroResponse(int IdLibro, string Isbn, string Titulo, string Autor, string Categoria);

public sealed class CrearLibroValidator : AbstractValidator<CrearLibroRequest>
{
    public CrearLibroValidator()
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

public static class CrearLibroHandler
{
    public static async Task<Result<CrearLibroResponse>> Handle(
        CrearLibroRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var isbn = LibroIsbn.Limpiar(request.Isbn);
        var isbnExiste = await db.Libros.AnyAsync(libro => libro.Isbn == isbn, cancellationToken);
        if (isbnExiste)
        {
            return IsbnDuplicado();
        }

        var libro = new Libro
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
        catch (DbUpdateException exception) when (EsIsbnDuplicado(exception))
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

    private static bool EsIsbnDuplicado(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}

public sealed class CrearLibroEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros")
            .WithTags("Libros")
            .MapPost("", async (
                CrearLibroRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await CrearLibroHandler.Handle(request, db, cancellationToken);
                return result.IsSuccess
                    ? result.ToHttpResult($"/api/libros/{result.Value!.IdLibro}")
                    : result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CrearLibroRequest>>()
            .Produces<CrearLibroResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
