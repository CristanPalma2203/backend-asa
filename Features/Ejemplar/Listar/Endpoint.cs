using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Ejemplar.Listar;

public sealed class ListarEjemplaresEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros/{idLibro:int}/ejemplares")
            .WithTags("Ejemplares")
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .MapGet("", async (
                [AsParameters] ListarEjemplaresRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var ejemplares = await ListarEjemplaresHandler.Handle(request.IdLibro, db, cancellationToken);
                return ejemplares.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ListarEjemplaresRequest>>()
            .Produces<IReadOnlyList<ListarEjemplaresItem>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
