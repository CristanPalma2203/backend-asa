using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Lector.Obtener;

public sealed class ObtenerLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapGet("/{id:int}", async (
                [AsParameters] ObtenerLectorRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ObtenerLectorHandler.Handle(request.Id, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ObtenerLectorRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces<ObtenerLectorResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
