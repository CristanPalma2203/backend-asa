using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Ejemplar.Editar;

public sealed class EditarEjemplarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/ejemplares")
            .WithTags("Ejemplares")
            .MapPut("/{id:int}", async (
                int id,
                EditarEjemplarRequest request,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await EditarEjemplarHandler.Handle(id, request, db, timeProvider, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<EditarEjemplarRequest>>()
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
