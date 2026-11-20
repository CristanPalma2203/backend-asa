using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lectores;

public sealed record ObtenerLectorRequest(int Id);

public sealed record ObtenerLectorResponse(
    int IdLector,
    string Identificador,
    string Nombre,
    string Correo,
    EstadoLector Estado,
    bool TieneUsuario,
    int ReservasActivas);

public sealed class ObtenerLectorValidator : AbstractValidator<ObtenerLectorRequest>
{
    public ObtenerLectorValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");
    }
}

public static class ObtenerLectorHandler
{
    public static async Task<Result<ObtenerLectorResponse>> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var lector = await db.Lectores
            .AsNoTracking()
            .Where(item => item.IdLector == id)
            .Select(item => new ObtenerLectorResponse(
                item.IdLector,
                item.Identificador,
                item.Nombre,
                item.Correo,
                item.Estado,
                db.Usuarios.Any(usuario => usuario.IdLector == item.IdLector),
                item.Reservas.Count(reserva => reserva.Estado == EstadoReserva.Activa)))
            .FirstOrDefaultAsync(cancellationToken);

        if (lector is null)
        {
            return Result.Failure<ObtenerLectorResponse>(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        return Result.Success(lector);
    }
}

public sealed class ObtenerLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapGet("/{id:int}", async (
                [AsParameters] ObtenerLectorRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await ObtenerLectorHandler.Handle(request.Id, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<ObtenerLectorRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces<ObtenerLectorResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
