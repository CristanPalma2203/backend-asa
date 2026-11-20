using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Pagination;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Reservas;

public sealed record ListarReservasPorLectorRequest(
    int LectorId,
    EstadoReserva? Estado,
    int Page = 1,
    int PageSize = 10);

public sealed class ListarReservasPorLectorValidator : AbstractValidator<ListarReservasPorLectorRequest>
{
    public ListarReservasPorLectorValidator()
    {
        RuleFor(x => x.LectorId)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");

        RuleFor(x => x.Estado)
            .IsInEnum()
            .WithMessage("El estado de la reserva no es válido.")
            .When(x => x.Estado is not null);
    }
}

public static class ListarReservasPorLectorHandler
{
    public static async Task<Result<PagedResponse<ReservaDetalle>>> Handle(
        ListarReservasPorLectorRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var lectorExiste = await db.Lectores
            .AsNoTracking()
            .AnyAsync(lector => lector.IdLector == request.LectorId, cancellationToken);

        if (!lectorExiste)
        {
            return Result.Failure<PagedResponse<ReservaDetalle>>(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        var query = db.Reservas
            .AsNoTracking()
            .Where(reserva => reserva.IdLector == request.LectorId);

        if (request.Estado is not null)
        {
            query = query.Where(reserva => reserva.Estado == request.Estado);
        }

        var proyectada = query
            .OrderByDescending(reserva => reserva.FechaSolicitud)
            .Select(reserva => new ReservaDetalle(
                reserva.IdReserva,
                reserva.Ejemplar.IdLibro,
                reserva.Ejemplar.Libro.Titulo,
                reserva.Ejemplar.CodigoInventario,
                reserva.Estado,
                reserva.FechaSolicitud,
                reserva.FechaCancelacion));

        var pagina = await proyectada.ToPagedResponseAsync(
            new PagedRequest(request.Page, request.PageSize),
            cancellationToken);

        return Result.Success(pagina);
    }
}

public sealed class ListarReservasPorLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores/{lectorId:int}/reservas")
            .WithTags("Reservas")
            .MapGet("", async (
                [AsParameters] ListarReservasPorLectorRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ListarReservasPorLectorHandler.Handle(request, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ListarReservasPorLectorRequest>>()
            .Produces<PagedResponse<ReservaDetalle>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
