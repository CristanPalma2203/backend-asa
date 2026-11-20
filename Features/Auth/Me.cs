using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend_asa.Common.Endpoints;

namespace backend_asa.Features.Auth;

public sealed class MeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/auth")
            .WithTags("Auth")
            .MapGet("/me", (ClaimsPrincipal usuario, IReadOnlyList<DemoUserOptions> usuarios) =>
            {
                var correo = usuario.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? usuario.FindFirst("email")?.Value
                    ?? string.Empty;
                var demo = usuarios.FirstOrDefault(item =>
                    string.Equals(item.Correo, correo, StringComparison.OrdinalIgnoreCase));
                var expira = FechaExpira(usuario);

                return Results.Ok(new AuthSession(
                    null,
                    expira,
                    usuario.FindFirst("role")?.Value ?? string.Empty,
                    demo?.Nombre ?? string.Empty,
                    Acceso.LectorId(usuario)));
            })
            .Produces<AuthSession>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static DateTimeOffset? FechaExpira(ClaimsPrincipal usuario)
    {
        var exp = usuario.FindFirst(JwtRegisteredClaimNames.Exp)?.Value ?? usuario.FindFirst("exp")?.Value;
        return long.TryParse(exp, out var unix)
            ? DateTimeOffset.FromUnixTimeSeconds(unix)
            : null;
    }
}
