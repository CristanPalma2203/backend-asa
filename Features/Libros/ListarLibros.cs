using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Pagination;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libros;

public sealed record ListarLibrosRequest(string? Buscar, string? Categoria, int Page = 1, int PageSize = 10);

public sealed record ListarLibrosItem(
    int IdLibro,
    string Isbn,
    string Titulo,
    string Autor,
    string Categoria,
    int TotalEjemplares,
    int Disponibles);

public sealed class ListarLibrosValidator : AbstractValidator<ListarLibrosRequest>
{
    public ListarLibrosValidator()
    {
        RuleFor(x => x.Buscar)
            .MaximumLength(200)
            .WithMessage("La búsqueda no puede superar 200 caracteres.")
            .When(x => x.Buscar is not null);

        RuleFor(x => x.Categoria)
            .MaximumLength(100)
            .WithMessage("La categoría no puede superar 100 caracteres.")
            .When(x => x.Categoria is not null);
    }
}

public static class ListarLibrosHandler
{
    public static async Task<Result<PagedResponse<ListarLibrosItem>>> Handle(
        ListarLibrosRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Libros
            .AsNoTracking()
            .Where(libro => libro.Estado == EstadoLibro.Activo);

        if (!string.IsNullOrWhiteSpace(request.Buscar))
        {
            var buscar = request.Buscar.Trim();
            query = query.Where(libro =>
                libro.Titulo.Contains(buscar) ||
                libro.Autor.Contains(buscar) ||
                libro.Isbn.Contains(buscar));
        }

        if (!string.IsNullOrWhiteSpace(request.Categoria))
        {
            var categoria = request.Categoria.Trim();
            query = query.Where(libro => libro.Categoria == categoria);
        }

        var proyectada = query
            .OrderBy(libro => libro.Titulo)
            .Select(libro => new ListarLibrosItem(
                libro.IdLibro,
                libro.Isbn,
                libro.Titulo,
                libro.Autor,
                libro.Categoria,
                libro.Ejemplares.Count(),
                libro.Ejemplares.Count(ejemplar => ejemplar.Estado == EstadoEjemplar.Disponible)));

        var pagina = await proyectada.ToPagedResponseAsync(
            new PagedRequest(request.Page, request.PageSize),
            cancellationToken);

        return Result.Success(pagina);
    }
}

public sealed class ListarLibrosEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros")
            .WithTags("Libros")
            .MapGet("", async (
                [AsParameters] ListarLibrosRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ListarLibrosHandler.Handle(request, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ListarLibrosRequest>>()
            .Produces<PagedResponse<ListarLibrosItem>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
