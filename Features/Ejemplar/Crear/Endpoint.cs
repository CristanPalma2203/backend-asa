using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Ejemplar.Crear;

public sealed class CrearEjemplarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros/{idLibro:int}/ejemplares")
            .WithTags("Ejemplares")
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .MapPost("", async (
                int idLibro,
                CrearEjemplarRequest request,
                BibliotecaDbContext db,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var ejemplarCreado = await CrearEjemplarHandler.Handle(idLibro, request, db, timeProvider, cancellationToken);
                return ejemplarCreado.IsSuccess
                    ? ejemplarCreado.ToHttpResult($"/api/libros/{idLibro}/ejemplares/{ejemplarCreado.Value!.IdEjemplar}")
                    : ejemplarCreado.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CrearEjemplarRequest>>()
            .Produces<CrearEjemplarResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
