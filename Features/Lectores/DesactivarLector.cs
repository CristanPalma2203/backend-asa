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

public sealed record DesactivarLectorRequest(int Id);

public sealed class DesactivarLectorValidator : AbstractValidator<DesactivarLectorRequest>
{
    public DesactivarLectorValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del lector no es válido.");
    }
}

public static class DesactivarLectorHandler
{
    public static async Task<Result> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var lector = await db.Lectores.FirstOrDefaultAsync(item => item.IdLector == id, cancellationToken);
        if (lector is null)
        {
            return Result.Failure(new Error(
                "lector-no-encontrado",
                "No se encontró el lector.",
                ErrorType.NotFound));
        }

        var tieneReservasActivas = await db.Reservas.AnyAsync(
            reserva => reserva.IdLector == id && reserva.Estado == EstadoReserva.Activa,
            cancellationToken);

        if (tieneReservasActivas)
        {
            return Result.Failure(new Error(
                "lector-con-reservas-activas",
                "El lector tiene reservas activas y no se puede desactivar.",
                ErrorType.BusinessRule));
        }

        lector.Estado = EstadoLector.Inactivo;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(item => item.IdLector == id, cancellationToken);
        if (usuario is not null)
        {
            usuario.Estado = EstadoUsuario.Inactivo;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class DesactivarLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapDelete("/{id:int}", async (
                [AsParameters] DesactivarLectorRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await DesactivarLectorHandler.Handle(request.Id, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<DesactivarLectorRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
