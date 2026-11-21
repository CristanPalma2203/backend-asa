using System.Security.Claims;
using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Reserva.Crear;

public sealed class CrearReservaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/reservas")
            .WithTags("Reservas")
            .MapPost("", async (
                CrearReservaRequest request,
                ClaimsPrincipal usuario,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var reservaCreada = await CrearReservaHandler.Handle(request, usuario, db, timeProvider, cancellationToken);
                return reservaCreada.IsSuccess
                    ? reservaCreada.ToHttpResult($"/api/reservas/{reservaCreada.Value!.IdReserva}")
                    : reservaCreada.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CrearReservaRequest>>()
            .Produces<CrearReservaResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
