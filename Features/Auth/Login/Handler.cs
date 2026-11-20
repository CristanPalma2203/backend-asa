using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_asa.Common.Results;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend_asa.Features.Auth.Login;

public static class LoginHandler
{
    public static async Task<Result<AuthSession>> Handle(
        LoginRequest request,
        BibliotecaDbContext db,
        IOptions<JwtOptions> jwt,
        PasswordHasher<Usuario> hasher,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var correo = request.Correo.Trim();
        var usuario = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Correo == correo && item.Estado == EstadoUsuario.Activo,
                cancellationToken);

        if (usuario is null
            || hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return CredencialesInvalidas();
        }

        var lectorId = usuario.IdLector;

        var opciones = jwt.Value;
        var expira = timeProvider.GetUtcNow().AddMinutes(opciones.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Correo),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new("role", usuario.Rol.ToString())
        };

        if (lectorId is int id)
        {
            claims.Add(new Claim(Acceso.LectorIdClaim, id.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: opciones.Issuer,
            audience: opciones.Audience,
            claims: claims,
            notBefore: timeProvider.GetUtcNow().UtcDateTime,
            expires: expira.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Key)),
                SecurityAlgorithms.HmacSha256));

        return Result.Success(new AuthSession(
            new JwtSecurityTokenHandler().WriteToken(token),
            expira,
            usuario.Rol.ToString(),
            usuario.Nombre,
            lectorId));
    }

    private static Result<AuthSession> CredencialesInvalidas() =>
        Result.Failure<AuthSession>(new Error(
            "credenciales-invalidas",
            "El correo o la contraseña no son válidos.",
            ErrorType.Unauthorized));
}
