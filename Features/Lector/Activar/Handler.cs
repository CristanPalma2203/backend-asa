using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lector.Activar;

public static class ActivarLectorHandler
{
    public static async Task<Result> Handle(
        int id,
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

        // Simétrico a Desactivar: también reactiva su usuario, si tiene, para que pueda volver a entrar.
        // Idempotente: si ya está activo, no cambia nada y responde igual.
        lector.Estado = EstadoLector.Activo;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(item => item.IdLector == id, cancellationToken);
        if (usuario is not null)
        {
            usuario.Estado = EstadoUsuario.Activo;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
