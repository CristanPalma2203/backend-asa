using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Ejemplar.CambiarEstado;

public sealed class CambiarEstadoEjemplarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros/{idLibro:int}/ejemplares")
            .WithTags("Ejemplares")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .MapPut("/{idEjemplar:int}/estado", async (
                int idLibro,
                int idEjemplar,
                CambiarEstadoEjemplarRequest request,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await CambiarEstadoEjemplarHandler.Handle(idLibro, idEjemplar, request, db, timeProvider, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CambiarEstadoEjemplarRequest>>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
