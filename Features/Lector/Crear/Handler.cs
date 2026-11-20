using backend_asa.Common.Results;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lector.Crear;

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

        var lector = new Domain.Lector
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
