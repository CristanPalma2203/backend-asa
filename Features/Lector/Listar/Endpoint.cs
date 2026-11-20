using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Pagination;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;

namespace backend_asa.Features.Lector.Listar;

public sealed class ListarLectoresEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapGet("", async (
                [AsParameters] ListarLectoresRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ListarLectoresHandler.Handle(request, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ListarLectoresRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces<PagedResponse<ListarLectoresItem>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
