using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Resumen.Obtener;

public sealed class ObtenerResumenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/resumen")
            .WithTags("Resumen")
            .MapGet("", async (
                [AsParameters] ObtenerResumenRequest request,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var resumen = await ObtenerResumenHandler.Handle(request, db, timeProvider, cancellationToken);
                return resumen.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ObtenerResumenRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces<ObtenerResumenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
