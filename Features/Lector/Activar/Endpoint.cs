using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Lector.Activar;

public sealed class ActivarLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapPost("/{id:int}/activar", async (
                [AsParameters] ActivarLectorRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var activacion = await ActivarLectorHandler.Handle(request.Id, db, cancellationToken);
                return activacion.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ActivarLectorRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
