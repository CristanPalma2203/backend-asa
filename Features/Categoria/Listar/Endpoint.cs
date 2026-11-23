using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Categoria.Listar;

public sealed class ListarCategoriasEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/categorias")
            .WithTags("Categorías")
            .MapGet("", async (BibliotecaDbContext db, CancellationToken cancellationToken) =>
            {
                var categorias = await ListarCategoriasHandler.Handle(db, cancellationToken);
                return categorias.ToHttpResult();
            })
            .AllowAnonymous()
            .Produces<IReadOnlyList<CategoriaItem>>(StatusCodes.Status200OK);
    }
}
