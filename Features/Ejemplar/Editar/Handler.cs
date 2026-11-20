using backend_asa.Common.Results;
using backend_asa.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplar.Editar;

public static class EditarEjemplarHandler
{
    public static async Task<Result> Handle(
        int id,
        EditarEjemplarRequest request,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var ejemplar = await db.Ejemplares.FirstOrDefaultAsync(item => item.IdEjemplar == id, cancellationToken);
        if (ejemplar is null)
        {
            return Result.Failure(new Error(
                "ejemplar-no-encontrado",
                "No se encontró el ejemplar.",
                ErrorType.NotFound));
        }

        var codigo = CodigoInventario.Normalizar(request.CodigoInventario);
        var codigoOcupado = await db.Ejemplares.AnyAsync(
            item => item.CodigoInventario == codigo && item.IdEjemplar != id,
            cancellationToken);
        if (codigoOcupado)
        {
            return CodigoDuplicado();
        }

        // Si otro usuario cambió el ejemplar después de que el cliente lo leyó, EF lanza
        // DbUpdateConcurrencyException y GlobalExceptionHandler responde 409 conflicto-concurrencia.
        RowVersionToken.TryRead(request.RowVersion, out var rowVersion);
        db.Entry(ejemplar).Property(item => item.RowVersion).OriginalValue = rowVersion;
        ejemplar.CodigoInventario = codigo;
        ejemplar.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsCodigoDuplicado(exception))
        {
            return CodigoDuplicado();
        }

        return Result.Success();
    }

    private static Result CodigoDuplicado() =>
        Result.Failure(new Error(
            "codigo-inventario-duplicado",
            "Ya existe un ejemplar con ese código de inventario.",
            ErrorType.Conflict));

    private static bool EsCodigoDuplicado(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}
