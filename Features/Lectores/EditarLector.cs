using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lectores;

public sealed record EditarLectorRequest(string Identificador, string Nombre, string Correo);

public sealed class EditarLectorValidator : AbstractValidator<EditarLectorRequest>
{
    public EditarLectorValidator()
    {
        RuleFor(x => x.Identificador)
            .NotEmpty().WithMessage("El identificador es obligatorio.")
            .MaximumLength(20).WithMessage("El identificador no puede superar 20 caracteres.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(150).WithMessage("El correo no puede superar 150 caracteres.")
            .EmailAddress().WithMessage("El correo no es válido.");
    }
}

public static class EditarLectorHandler
{
    public static async Task<Result> Handle(
        int id,
        EditarLectorRequest request,
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

        var identificador = request.Identificador.Trim();
        var correo = request.Correo.Trim();

        if (await db.Lectores.AnyAsync(item => item.Correo == correo && item.IdLector != id, cancellationToken)
            || await db.Usuarios.AnyAsync(item => item.Correo == correo && item.IdLector != id, cancellationToken))
        {
            return CorreoDuplicado();
        }

        if (await db.Lectores.AnyAsync(item => item.Identificador == identificador && item.IdLector != id, cancellationToken))
        {
            return IdentificadorDuplicado();
        }

        lector.Identificador = identificador;
        lector.Nombre = request.Nombre.Trim();
        lector.Correo = correo;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(item => item.IdLector == id, cancellationToken);
        if (usuario is not null)
        {
            usuario.Correo = correo;
            usuario.Nombre = lector.Nombre;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            return await db.Lectores.AnyAsync(item => item.Identificador == identificador && item.IdLector != id, cancellationToken)
                ? IdentificadorDuplicado()
                : CorreoDuplicado();
        }

        return Result.Success();
    }

    private static Result CorreoDuplicado() =>
        Result.Failure(new Error(
            "correo-duplicado",
            "Ya existe un lector o un usuario con ese correo.",
            ErrorType.Conflict));

    private static Result IdentificadorDuplicado() =>
        Result.Failure(new Error(
            "identificador-duplicado",
            "Ya existe un lector con ese identificador.",
            ErrorType.Conflict));
}

public sealed class EditarLectorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/lectores")
            .WithTags("Lectores")
            .MapPut("/{id:int}", async (
                int id,
                EditarLectorRequest request,
                BibliotecaDbContext db,
                CancellationToken cancellationToken) =>
            {
                var result = await EditarLectorHandler.Handle(id, request, db, cancellationToken);
                return result.ToHttpResult();
            })
            .AddEndpointFilter<ValidationFilter<EditarLectorRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(Acceso.RolAdmin))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
