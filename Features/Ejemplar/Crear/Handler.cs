using backend_asa.Common.Persistence;
using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Ejemplar.Crear;

public static class CrearEjemplarHandler
{
    public static async Task<Result<CrearEjemplarResponse>> Handle(
        int idLibro,
        CrearEjemplarRequest request,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var libro = await db.Libros
            .AsNoTracking()
            .Where(item => item.IdLibro == idLibro)
            .Select(item => new { item.Estado })
            .FirstOrDefaultAsync(cancellationToken);

        if (libro is null)
        {
            return Result.Failure<CrearEjemplarResponse>(new Error(
                "libro-no-encontrado",
                "No se encontró el libro.",
                ErrorType.NotFound));
        }

        if (libro.Estado == EstadoLibro.Inactivo)
        {
            return Result.Failure<CrearEjemplarResponse>(new Error(
                "libro-inactivo",
                "El libro está inactivo y no admite ejemplares.",
                ErrorType.BusinessRule));
        }

        var codigo = CodigoInventario.Normalizar(request.CodigoInventario);
        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var ejemplar = new Domain.Ejemplar
        {
            IdLibro = idLibro,
            CodigoInventario = codigo,
            Estado = EstadoEjemplar.Disponible,
            CreatedAt = ahora,
            UpdatedAt = ahora
        };

        db.Ejemplares.Add(ejemplar);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.EsViolacionDeIndiceUnico())
        {
            return CodigoDuplicado();
        }

        return Result.Success(new CrearEjemplarResponse(
            ejemplar.IdEjemplar,
            ejemplar.CodigoInventario,
            ejemplar.Estado,
            Convert.ToBase64String(ejemplar.RowVersion)));
    }

    private static Result<CrearEjemplarResponse> CodigoDuplicado() =>
        Result.Failure<CrearEjemplarResponse>(new Error(
            "codigo-inventario-duplicado",
            "Ya existe un ejemplar con ese código de inventario.",
            ErrorType.Conflict));
}
