using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Libro.Desactivar;

public sealed class DesactivarLibroEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros")
            .WithTags("Libros")
            .MapDelete("/{id:int}", async (
                [AsParameters] DesactivarLibroRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await DesactivarLibroHandler.Handle(request.Id, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<DesactivarLibroRequest>>()
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
