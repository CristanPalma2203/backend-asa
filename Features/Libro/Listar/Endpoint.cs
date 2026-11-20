using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Pagination;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Libro.Listar;

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
            .AllowAnonymous()
            .Produces<PagedResponse<ListarLibrosItem>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
