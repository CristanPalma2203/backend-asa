using System.Security.Claims;
using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Reserva.Obtener;

public sealed class ObtenerReservaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/reservas")
            .WithTags("Reservas")
            .MapGet("/{id:int}", async (
                [AsParameters] ObtenerReservaRequest request,
                ClaimsPrincipal usuario,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var reserva = await ObtenerReservaHandler.Handle(request.Id, usuario, db, cancellationToken);
                return reserva.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ObtenerReservaRequest>>()
            .Produces<ReservaDetalle>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
