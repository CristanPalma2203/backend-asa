using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend_asa.Features.Auth;

public sealed record LoginRequest(string Correo, string Password);

public sealed record AuthSession(string? AccessToken, DateTimeOffset? ExpiresAt, string Rol, string Nombre, int? LectorId);

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no es válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

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
                var result = await LoginHandler.Handle(request, db, jwt, hasher, timeProvider, cancellationToken);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .Produces<AuthSession>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
