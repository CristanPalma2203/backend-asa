using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Reservas;

public sealed record ObtenerReservaRequest(int Id);

public sealed class ObtenerReservaValidator : AbstractValidator<ObtenerReservaRequest>
{
    public ObtenerReservaValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador de la reserva no es válido.");
    }
}

public static class ObtenerReservaHandler
{
    public static async Task<Result<ReservaDetalle>> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var reserva = await db.Reservas
            .AsNoTracking()
            .Where(item => item.IdReserva == id)
            .Select(item => new ReservaDetalle(
                item.IdReserva,
                item.Ejemplar.IdLibro,
                item.Ejemplar.Libro.Titulo,
                item.Ejemplar.CodigoInventario,
                item.Estado,
                item.FechaSolicitud,
                item.FechaCancelacion))
            .FirstOrDefaultAsync(cancellationToken);

        if (reserva is null)
        {
            return Result.Failure<ReservaDetalle>(new Error(
                "reserva-no-encontrada",
                "No se encontró la reserva.",
                ErrorType.NotFound));
        }

        return Result.Success(reserva);
    }
}

public sealed class ObtenerReservaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/reservas")
            .WithTags("Reservas")
            .MapGet("/{id:int}", async (
                [AsParameters] ObtenerReservaRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ObtenerReservaHandler.Handle(request.Id, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ObtenerReservaRequest>>()
            .Produces<ReservaDetalle>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
