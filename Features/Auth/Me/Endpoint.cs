using System.Security.Claims;
using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Auth.Me;

public sealed class MeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/auth")
            .WithTags("Auth")
            .MapGet("/me", async (ClaimsPrincipal usuario, BibliotecaDbContext db, CancellationToken cancellationToken) =>
            {
                var sesion = await MeHandler.Handle(usuario, db, cancellationToken);
                return sesion.ToHttpResult();
            })
            .Produces<MeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
