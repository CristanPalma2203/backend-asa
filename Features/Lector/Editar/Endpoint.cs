using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Lector.Editar;

public sealed class EditarLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapPut("/{id:int}", async (
                int id,
                EditarLectorRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await EditarLectorHandler.Handle(id, request, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<EditarLectorRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
