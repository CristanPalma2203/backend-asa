using System.Security.Claims;
using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using backend_asa.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Reserva.Crear;

public static class CrearReservaHandler
{
    public static async Task<Result<CrearReservaResponse>> Handle(
        CrearReservaRequest request,
        ClaimsPrincipal usuario,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var estrategiaReintentos = db.Database.CreateExecutionStrategy();
        return await estrategiaReintentos.ExecuteAsync(
            async ct =>
            {
                db.ChangeTracker.Clear();
                return await EjecutarIntentoAsync(request, usuario, db, timeProvider, ct);
            },
            cancellationToken);
    }

    private static async Task<Result<CrearReservaResponse>> EjecutarIntentoAsync(
        CrearReservaRequest request,
        ClaimsPrincipal usuario,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var acceso = Acceso.ResolverLector(usuario, request.LectorId);
        if (!acceso.IsSuccess)
        {
            return Result.Failure<CrearReservaResponse>(acceso.Error);
        }

        var lectorId = acceso.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted,
            cancellationToken);

        var lectorBloqueado = await db.Lectores
            .FromSql($"SELECT * FROM Lectores WITH (UPDLOCK, ROWLOCK) WHERE IdLector = {lectorId}")
            .Select(item => new { item.Estado })
            .FirstOrDefaultAsync(cancellationToken);

        if (lectorBloqueado is null)
        {
            return Result.Failure<CrearReservaResponse>(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        if (lectorBloqueado.Estado == EstadoLector.Inactivo)
        {
            return Result.Failure<CrearReservaResponse>(new Error(
                "lector-inactivo",
                "El lector está inactivo y no puede reservar.",
                ErrorType.BusinessRule));
        }

        var libro = await db.Libros
            .AsNoTracking()
            .Where(item => item.IdLibro == request.LibroId)
            .Select(item => new { item.Estado })
            .FirstOrDefaultAsync(cancellationToken);

        if (libro is null)
        {
            return Result.Failure<CrearReservaResponse>(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        if (libro.Estado == EstadoLibro.Inactivo)
        {
            return Result.Failure<CrearReservaResponse>(new Error(
                "libro-inactivo",
                "El libro está inactivo y no admite reservas.",
                ErrorType.BusinessRule));
        }

        var reservaDuplicada = await db.Reservas.AnyAsync(
            reserva => reserva.IdLector == lectorId
                && reserva.Estado == EstadoReserva.Activa
                && reserva.Ejemplar.IdLibro == request.LibroId,
            cancellationToken);

        if (reservaDuplicada)
        {
            return Result.Failure<CrearReservaResponse>(new Error(
                "reserva-duplicada",
                "El lector ya tiene una reserva activa de ese libro.",
                ErrorType.BusinessRule));
        }

        var ejemplar = await db.Ejemplares
            .FromSql(
                $"""
                SELECT TOP (1) e.*
                FROM Ejemplares AS e WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE e.IdLibro = {request.LibroId} AND e.Estado = 'Disponible'
                ORDER BY e.CodigoInventario
                """)
            .FirstOrDefaultAsync(cancellationToken);

        if (ejemplar is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<CrearReservaResponse>(new Error(
                "sin-stock",
                "No hay ejemplares disponibles para ese libro.",
                ErrorType.Conflict));
        }

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var reserva = new Domain.Reserva
        {
            IdLector = lectorId,
            IdEjemplar = ejemplar.IdEjemplar,
            Estado = EstadoReserva.Activa,
            FechaSolicitud = ahora
        };

        ejemplar.Estado = EstadoEjemplar.Reservado;
        ejemplar.UpdatedAt = ahora;
        db.Reservas.Add(reserva);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.EsViolacionDeIndiceUnico())
        {
            return Result.Failure<CrearReservaResponse>(new Error(
                "ejemplar-ya-reservado",
                "El ejemplar ya tiene una reserva activa.",
                ErrorType.Conflict));
        }

        return Result.Success(new CrearReservaResponse(
            reserva.IdReserva,
            ejemplar.IdEjemplar,
            ejemplar.CodigoInventario,
            reserva.Estado,
            reserva.FechaSolicitud));
    }
}
