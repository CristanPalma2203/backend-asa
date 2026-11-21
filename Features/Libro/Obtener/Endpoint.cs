using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Libro.Obtener;

public sealed class ObtenerLibroEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros")
            .WithTags("Libros")
            .MapGet("/{id:int}", async (
                [AsParameters] ObtenerLibroRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var libro = await ObtenerLibroHandler.Handle(request.Id, db, cancellationToken);
                return libro.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ObtenerLibroRequest>>()
            .AllowAnonymous()
            .Produces<ObtenerLibroResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
