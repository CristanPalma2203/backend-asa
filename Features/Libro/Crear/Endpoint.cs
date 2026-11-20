using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Libro.Crear;

public sealed class CrearLibroEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros")
            .WithTags("Libros")
            .MapPost("", async (
                CrearLibroRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await CrearLibroHandler.Handle(request, db, cancellationToken);
                return result.IsSuccess
                    ? result.ToHttpResult($"/api/libros/{result.Value!.IdLibro}")
                    : result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CrearLibroRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces<CrearLibroResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
