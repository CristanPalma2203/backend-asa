using System.Security.Claims;
using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Reserva.Cancelar;

public sealed class CancelarReservaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/reservas")
            .WithTags("Reservas")
            .MapPost("/{id:int}/cancelar", async (
                [AsParameters] CancelarReservaRequest request,
                ClaimsPrincipal usuario,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var cancelacion = await CancelarReservaHandler.Handle(request.Id, usuario, db, timeProvider, cancellationToken);
                return cancelacion.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CancelarReservaRequest>>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
