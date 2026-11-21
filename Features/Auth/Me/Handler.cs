using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend_asa.Common.Results;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Auth.Me;

public static class MeHandler
{
    public static async Task<Result<MeResponse>> Handle(
        ClaimsPrincipal usuario,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var correo = usuario.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        var nombre = await db.Usuarios
            .AsNoTracking()
            .Where(item => item.Correo == correo)
            .Select(item => item.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(new MeResponse(
            null,
            FechaExpira(usuario),
            usuario.FindFirst("role")?.Value ?? string.Empty,
            nombre ?? string.Empty,
            Acceso.LectorId(usuario)));
    }

    private static DateTimeOffset? FechaExpira(ClaimsPrincipal usuario) =>
        long.TryParse(usuario.FindFirst(JwtRegisteredClaimNames.Exp)?.Value, out var unix)
            ? DateTimeOffset.FromUnixTimeSeconds(unix)
            : null;
}
