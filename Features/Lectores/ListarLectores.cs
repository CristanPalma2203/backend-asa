using backend_asa.Common.Endpoints;
using backend_asa.Common.Http;
using backend_asa.Common.Pagination;
using backend_asa.Common.Results;
using backend_asa.Common.Validation;
using backend_asa.Domain.Enums;
using backend_asa.Features.Auth;
using backend_asa.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Lectores;

public sealed record ListarLectoresRequest(string? Buscar, EstadoLector? Estado, int Page = 1, int PageSize = 10);

public sealed record ListarLectoresItem(
    int IdLector,
    string Identificador,
    string Nombre,
    string Correo,
    EstadoLector Estado,
    bool TieneUsuario);

public sealed class ListarLectoresValidator : AbstractValidator<ListarLectoresRequest>
{
    public ListarLectoresValidator()
    {
        RuleFor(x => x.Buscar)
            .MaximumLength(150)
            .WithMessage("La búsqueda no puede superar 150 caracteres.")
            .When(x => x.Buscar is not null);

        RuleFor(x => x.Estado)
            .IsInEnum()
            .WithMessage("El estado del lector no es válido.")
            .When(x => x.Estado is not null);
    }
}

public static class ListarLectoresHandler
{
    public static async Task<Result<PagedResponse<ListarLectoresItem>>> Handle(
        ListarLectoresRequest request,
        BibliotecaDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Lectores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Buscar))
        {
            var buscar = request.Buscar.Trim();
            query = query.Where(lector =>
                lector.Nombre.Contains(buscar) ||
                lector.Correo.Contains(buscar) ||
                lector.Identificador.Contains(buscar));
        }

        if (request.Estado is not null)
        {
            query = query.Where(lector => lector.Estado == request.Estado);
        }

        var proyectada = query
            .OrderBy(lector => lector.Nombre)
            .Select(lector => new ListarLectoresItem(
                lector.IdLector,
                lector.Identificador,
                lector.Nombre,
                lector.Correo,
                lector.Estado,
                db.Usuarios.Any(usuario => usuario.IdLector == lector.IdLector)));

        var pagina = await proyectada.ToPagedResponseAsync(
            new PagedRequest(request.Page, request.PageSize),
            cancellationToken);

        return Result.Success(pagina);
    }
}

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
