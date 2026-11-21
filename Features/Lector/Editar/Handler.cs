using backend_asa.Common.Persistence;
using backend_asa.Common.Results;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lector.Editar;

public static class EditarLectorHandler
{
    public static async Task<Result> Handle(
        int id,
        EditarLectorRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var lector = await db.Lectores.FirstOrDefaultAsync(item => item.IdLector == id, cancellationToken);
        if (lector is null)
        {
            return Result.Failure(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        var identificador = request.Identificador.Trim();
        var correo = request.Correo.Trim();

        if (await db.Lectores.AnyAsync(item => item.Correo == correo && item.IdLector != id, cancellationToken)
            || await db.Usuarios.AnyAsync(item => item.Correo == correo && item.IdLector != id, cancellationToken))
        {
            return CorreoDuplicado();
        }

        if (await db.Lectores.AnyAsync(item => item.Identificador == identificador && item.IdLector != id, cancellationToken))
        {
            return IdentificadorDuplicado();
        }

        lector.Identificador = identificador;
        lector.Nombre = request.Nombre.Trim();
        lector.Correo = correo;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(item => item.IdLector == id, cancellationToken);
        if (usuario is not null)
        {
            usuario.Correo = correo;
            usuario.Nombre = lector.Nombre;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.EsViolacionDeIndiceUnico())
        {
            return await db.Lectores.AnyAsync(item => item.Identificador == identificador && item.IdLector != id, cancellationToken)
                ? IdentificadorDuplicado()
                : CorreoDuplicado();
        }

        return Result.Success();
    }

    private static Result CorreoDuplicado() =>
        Result.Failure(new Error(
            "correo-duplicado",
            "Ya existe un lector o un usuario con ese correo.",
            ErrorType.Conflict));

    private static Result IdentificadorDuplicado() =>
        Result.Failure(new Error(
            "identificador-duplicado",
            "Ya existe un lector con ese identificador.",
            ErrorType.Conflict));
}
