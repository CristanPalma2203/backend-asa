using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libro.Desactivar;

public static class DesactivarLibroHandler
{
    public static async Task<Result> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var estrategiaReintentos = db.Database.CreateExecutionStrategy();
        return await estrategiaReintentos.ExecuteAsync(
            async ct =>
            {
                db.ChangeTracker.Clear();
                return await EjecutarIntentoAsync(id, db, ct);
            },
            cancellationToken);
    }

    private static async Task<Result> EjecutarIntentoAsync(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted,
            cancellationToken);

        var libro = await db.Libros.FirstOrDefaultAsync(item => item.IdLibro == id, cancellationToken);
        if (libro is null)
        {
            return Result.Failure(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        await db.Ejemplares
            .FromSql($"SELECT * FROM Ejemplares WITH (UPDLOCK, ROWLOCK) WHERE IdLibro = {id}")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var tieneReservasActivas = await db.Reservas.AnyAsync(
            reserva => reserva.Estado == EstadoReserva.Activa && reserva.Ejemplar.IdLibro == id,
            cancellationToken);

        if (tieneReservasActivas)
        {
            return Result.Failure(new Error(
                "libro-con-reservas-activas",
                "El libro tiene reservas activas y no se puede desactivar.",
                ErrorType.BusinessRule));
        }

        libro.Estado = EstadoLibro.Inactivo;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
