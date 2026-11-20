using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Domain;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace backend_asa.Features.Lector.Crear;

public sealed class CrearLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapPost("", async (
                CrearLectorRequest request,
                BibliotecaDbContext db,
                PasswordHasher<Usuario> hasher,
                TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await CrearLectorHandler.Handle(request, db, hasher, timeProvider, cancellationToken);
                return result.IsSuccess
                    ? result.ToHttpResult($"/api/lectores/{result.Value!.IdLector}")
                    : result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<CrearLectorRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces<CrearLectorResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
