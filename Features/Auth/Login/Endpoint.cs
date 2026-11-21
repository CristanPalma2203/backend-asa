using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Domain;
using backend_asa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace backend_asa.Features.Auth.Login;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/auth")
            .WithTags("Auth")
            .MapPost("/login", async (
                LoginRequest request,
                BibliotecaDbContext db,
                IOptions<JwtOptions> jwt,
                PasswordHasher<Usuario> hasher,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var sesion = await LoginHandler.Handle(request, db, jwt, hasher, timeProvider, cancellationToken);
                return sesion.ToHttpResult();
            })
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .Produces<AuthSession>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
