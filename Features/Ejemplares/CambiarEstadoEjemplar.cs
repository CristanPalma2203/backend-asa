using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplares;

public sealed record CambiarEstadoEjemplarRequest(EstadoEjemplar Estado, string RowVersion);

public sealed class CambiarEstadoEjemplarValidator : AbstractValidator<CambiarEstadoEjemplarRequest>
{
    public CambiarEstadoEjemplarValidator()
    {
        RuleFor(x => x.Estado)
            .IsInEnum()
            .WithMessage("El estado del ejemplar no es válido.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("La versión del registro es obligatoria.")
            .Must(RowVersionToken.EsValido)
            .WithMessage("La versión del registro no es válida.");
    }
}

internal static class RowVersionToken
{
    public static bool EsValido(string? token) => TryRead(token, out _);

    public static bool TryRead(string? token, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(token);
        }
        catch (FormatException)
        {
            return false;
        }

        return bytes.Length == 8;
    }
}

public static class CambiarEstadoEjemplarHandler
{
    public static async Task<Result> Handle(
        int idLibro,
        int idEjemplar,
        CambiarEstadoEjemplarRequest request,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (request.Estado is not (EstadoEjemplar.Disponible or EstadoEjemplar.Inactivo))
        {
            return Result.Failure(new Error(
                "estado-no-permitido",
                "Desde aquí solo se puede pasar el ejemplar a Disponible o Inactivo.",
                ErrorType.BusinessRule));
        }

        var ejemplar = await db.Ejemplares.FirstOrDefaultAsync(
            item => item.IdEjemplar == idEjemplar && item.IdLibro == idLibro,
            cancellationToken);

        if (ejemplar is null)
        {
            return Result.Failure(new Error(
                "ejemplar-no-encontrado",
                "No se encontró el ejemplar.",
                ErrorType.NotFound));
        }

        var reservado = ejemplar.Estado == EstadoEjemplar.Reservado
            || await db.Reservas.AnyAsync(
                reserva => reserva.IdEjemplar == idEjemplar && reserva.Estado == EstadoReserva.Activa,
                cancellationToken);

        if (reservado)
        {
            return Result.Failure(new Error(
                "ejemplar-reservado",
                "El ejemplar tiene una reserva activa y no se puede cambiar su estado.",
                ErrorType.BusinessRule));
        }

        if (ejemplar.Estado is not (EstadoEjemplar.Disponible or EstadoEjemplar.Inactivo))
        {
            return Result.Failure(new Error(
                "estado-no-permitido",
                "Desde aquí solo se puede cambiar un ejemplar Disponible o Inactivo.",
                ErrorType.BusinessRule));
        }

        RowVersionToken.TryRead(request.RowVersion, out var rowVersion);
        db.Entry(ejemplar).Property(item => item.RowVersion).OriginalValue = rowVersion;
        ejemplar.Estado = request.Estado;
        ejemplar.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CambiarEstadoEjemplarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros/{idLibro:int}/ejemplares")
            .WithTags("Ejemplares")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .MapPut("/{idEjemplar:int}/estado", async (
                int idLibro,
                int idEjemplar,
                CambiarEstadoEjemplarRequest request,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await CambiarEstadoEjemplarHandler.Handle(idLibro, idEjemplar, request, db, timeProvider, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CambiarEstadoEjemplarRequest>>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
