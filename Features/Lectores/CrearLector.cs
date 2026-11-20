using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lectores;

public sealed record CrearLectorRequest(string Identificador, string Nombre, string Correo, string? Password);

public sealed record CrearLectorResponse(
    int IdLector,
    string Identificador,
    string Nombre,
    string Correo,
    EstadoLector Estado,
    bool TieneUsuario);

public sealed class CrearLectorValidator : AbstractValidator<CrearLectorRequest>
{
    public CrearLectorValidator()
    {
        RuleFor(x => x.Identificador)
            .NotEmpty().WithMessage("El identificador es obligatorio.")
            .MaximumLength(20).WithMessage("El identificador no puede superar 20 caracteres.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(150).WithMessage("El correo no puede superar 150 caracteres.")
            .EmailAddress().WithMessage("El correo no es válido.");
    }
}

public static class CrearLectorHandler
{
    public static async Task<Result<CrearLectorResponse>> Handle(
        CrearLectorRequest request,
        BibliotecaDbContext db,
        PasswordHasher<Usuario> hasher,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var identificador = request.Identificador.Trim();
        var nombre = request.Nombre.Trim();
        var correo = request.Correo.Trim();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.Lectores.AnyAsync(lector => lector.Correo == correo, cancellationToken)
            || await db.Usuarios.AnyAsync(usuario => usuario.Correo == correo, cancellationToken))
        {
            return CorreoDuplicado();
        }

        if (await db.Lectores.AnyAsync(lector => lector.Identificador == identificador, cancellationToken))
        {
            return IdentificadorDuplicado();
        }

        var lector = new Lector
        {
            Identificador = identificador,
            Nombre = nombre,
            Correo = correo,
            Estado = EstadoLector.Activo
        };
        db.Lectores.Add(lector);

        var creaUsuario = !string.IsNullOrWhiteSpace(request.Password);
        if (creaUsuario)
        {
            var usuario = new Usuario
            {
                Correo = correo,
                Nombre = nombre,
                Rol = RolUsuario.Lector,
                Estado = EstadoUsuario.Activo,
                Lector = lector,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime
            };
            usuario.PasswordHash = hasher.HashPassword(usuario, request.Password!);
            db.Usuarios.Add(usuario);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsDuplicado(exception))
        {
            return await db.Lectores.AnyAsync(item => item.Identificador == identificador && item.IdLector != lector.IdLector, cancellationToken)
                ? IdentificadorDuplicado()
                : CorreoDuplicado();
        }

        return Result.Success(new CrearLectorResponse(
            lector.IdLector,
            lector.Identificador,
            lector.Nombre,
            lector.Correo,
            lector.Estado,
            creaUsuario));
    }

    private static Result<CrearLectorResponse> CorreoDuplicado() =>
        Result.Failure<CrearLectorResponse>(new Error(
            "correo-duplicado",
            "Ya existe un lector o un usuario con ese correo.",
            ErrorType.Conflict));

    private static Result<CrearLectorResponse> IdentificadorDuplicado() =>
        Result.Failure<CrearLectorResponse>(new Error(
            "identificador-duplicado",
            "Ya existe un lector con ese identificador.",
            ErrorType.Conflict));

    private static bool EsDuplicado(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}

public sealed class CrearLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapPost("", async (
                CrearLectorRequest request,
                BibliotecaDbContext db,
                PasswordHasher<Usuario> hasher,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await CrearLectorHandler.Handle(request, db, hasher, timeProvider, cancellationToken);
                return result.IsSuccess
                    ? result.ToHttpResult($"/api/lectores/{result.Value!.IdLector}")
                    : result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CrearLectorRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces<CrearLectorResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
