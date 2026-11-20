using System.Security.Claims;
using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Pagination;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Reserva.ListarPorLector;

public sealed class ListarReservasPorLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores/{lectorId:int}/reservas")
            .WithTags("Reservas")
            .MapGet("", async (
                [AsParameters] ListarReservasPorLectorRequest request,
                ClaimsPrincipal usuario,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ListarReservasPorLectorHandler.Handle(request, usuario, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ListarReservasPorLectorRequest>>()
            .Produces<PagedResponse<ReservaDetalle>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
