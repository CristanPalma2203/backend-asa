using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Reservas;

public sealed record CrearReservaRequest(int LectorId, int LibroId);

public sealed record CrearReservaResponse(
    int IdReserva,
    int IdEjemplar,
    string CodigoInventario,
    EstadoReserva Estado,
    DateTime FechaReserva);

public sealed class CrearReservaValidator : AbstractValidator<CrearReservaRequest>
{
    public CrearReservaValidator()
    {
        RuleFor(x => x.LectorId)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");

        RuleFor(x => x.LibroId)
            .GreaterThan(0)
            .WithMessage("El identificador del libro no es válido.");
    }
}

public static class CrearReservaHandler
{
    public static async Task<Result<CrearReservaResponse>> Handle(
        CrearReservaRequest request,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted,
            cancellationToken);

        var lector = await db.Lectores
            .AsNoTracking()
            .Where(item => item.IdLector == request.LectorId)
            .Select(item => new { item.Estado })
            .FirstOrDefaultAsync(cancellationToken);

        if (lector is null)
        {
            return Result.Failure<CrearReservaResponse>(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        if (lector.Estado == EstadoLector.Inactivo)
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
            reserva => reserva.IdLector == request.LectorId
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
        var reserva = new Reserva
        {
            IdLector = request.LectorId,
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
        catch (DbUpdateException exception) when (EsReservaDuplicada(exception))
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

    private static bool EsReservaDuplicada(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}

public sealed class CrearReservaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/reservas")
            .WithTags("Reservas")
            .MapPost("", async (
                CrearReservaRequest request,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await CrearReservaHandler.Handle(request, db, timeProvider, cancellationToken);
                return result.IsSuccess
                    ? result.ToHttpResult($"/api/reservas/{result.Value!.IdReserva}")
                    : result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CrearReservaRequest>>()
            .Produces<CrearReservaResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
