using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Libros;

public sealed record DesactivarLibroRequest(int Id);

public sealed class DesactivarLibroValidator : AbstractValidator<DesactivarLibroRequest>
{
    public DesactivarLibroValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El identificador del libro no es válido.");
    }
}

public static class DesactivarLibroHandler
{
    public static async Task<Result> Handle(
        int id,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var libro = await db.Libros.FirstOrDefaultAsync(item => item.IdLibro == id, cancellationToken);
        if (libro is null)
        {
            return Result.Failure(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        var tieneReservasActivas = await db.Reservas.AnyAsync(
            reserva => reserva.Estado == EstadoReserva.Activa && reserva.Ejemplar.IdLibro == id,
            cancellationToken);

        if (tieneReservasActivas)
        {
            return Result.Failure(new Error(
                "libro-con-reservas-activas",
                "El libro tiene reservas activas y no se puede desactivar.",
                ErrorType.BusinessRule));
        }

        libro.Estado = EstadoLibro.Inactivo;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class DesactivarLibroEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/libros")
            .WithTags("Libros")
            .MapDelete("/{id:int}", async (
                [AsParameters] DesactivarLibroRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await DesactivarLibroHandler.Handle(request.Id, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<DesactivarLibroRequest>>()
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
