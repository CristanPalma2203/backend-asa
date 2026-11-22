using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Validation;
using backend_asa.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace backend_asa.Features.Luxia.Preguntar;

public sealed class PreguntarLuxiaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/luxia")
            .WithTags("LuxIA")
            .MapPost("/preguntar", async (
                PreguntarLuxiaRequest request,
                MoonshotClient moonshot,
                IOptions<LuxiaOptions> opciones,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var respuesta = await PreguntarLuxiaHandler.Handle(request, moonshot, opciones, db, cancellationToken);
                return respuesta.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<PreguntarLuxiaRequest>>()
            .RequireRateLimiting(LuxiaOptions.PoliticaPorUsuario)
            .Produces<PreguntarLuxiaResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
