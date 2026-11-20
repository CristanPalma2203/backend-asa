using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend_asa.Common.Endpoints;
using backend_asa.Features.Auth.Login;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Auth.Me;

public sealed class MeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/auth")
            .WithTags("Auth")
            .MapGet("/me", async (ClaimsPrincipal usuario, BibliotecaDbContext db, CancellationToken cancellationToken) =>
            {
                var correo = usuario.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? usuario.FindFirst("email")?.Value
                    ?? string.Empty;
                var nombre = await db.Usuarios
                    .AsNoTracking()
                    .Where(item => item.Correo == correo)
                    .Select(item => item.Nombre)
                    .FirstOrDefaultAsync(cancellationToken);
                var expira = FechaExpira(usuario);

                return Results.Ok(new AuthSession(
                    null,
                    expira,
                    usuario.FindFirst("role")?.Value ?? string.Empty,
                    nombre ?? string.Empty,
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
