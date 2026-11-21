using backend_asa.Common.Results;
using backend_asa.Domain.Enums;
using backend_asa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Features.Resumen.Obtener;

public static class ObtenerResumenHandler
{
    private const int TopMasReservados = 5;
    private const int TopActividad = 10;
    private const int TopPorAgotarse = 8;

    public static async Task<Result<ObtenerResumenResponse>> Handle(
        ObtenerResumenRequest request,
        BibliotecaDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var primerDia = hoy.AddDays(-(request.Dias - 1));
        var inicioVentanaUtc = primerDia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var copiasDeLibrosActivos = db.Ejemplares.AsNoTracking().Where(e => e.Libro.Estado == EstadoLibro.Activo);

        var copiasPorEstado = await copiasDeLibrosActivos
            .GroupBy(e => e.Estado)
            .Select(g => new { Estado = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Estado, x => x.Total, cancellationToken);
        var activos = copiasPorEstado.Where(par => par.Key != EstadoEjemplar.Inactivo).Sum(par => par.Value);
        var disponibles = copiasPorEstado.GetValueOrDefault(EstadoEjemplar.Disponible);
        var reservados = copiasPorEstado.GetValueOrDefault(EstadoEjemplar.Reservado);

        var kpis = new ResumenKpis(
            await db.Libros.AsNoTracking().CountAsync(l => l.Estado == EstadoLibro.Activo, cancellationToken),
            activos,
            disponibles,
            reservados,
            await db.Lectores.AsNoTracking().CountAsync(l => l.Estado == EstadoLector.Activo, cancellationToken),
            await db.Reservas.AsNoTracking().CountAsync(r => r.Estado == EstadoReserva.Activa, cancellationToken),
            activos == 0 ? 0 : Math.Round((double)reservados / activos, 4));

        var reservasPorDia = await db.Reservas.AsNoTracking()
            .Where(r => r.FechaSolicitud >= inicioVentanaUtc)
            .GroupBy(r => r.FechaSolicitud.Date)
            .Select(g => new { Dia = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);
        var cancelacionesPorDia = await db.Reservas.AsNoTracking()
            .Where(r => r.FechaCancelacion != null && r.FechaCancelacion >= inicioVentanaUtc)
            .GroupBy(r => r.FechaCancelacion!.Value.Date)
            .Select(g => new { Dia = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);
        var reservasDia = reservasPorDia.ToDictionary(x => DateOnly.FromDateTime(x.Dia), x => x.Total);
        var cancelacionesDia = cancelacionesPorDia.ToDictionary(x => DateOnly.FromDateTime(x.Dia), x => x.Total);
        var serie = Enumerable.Range(0, request.Dias)
            .Select(i => primerDia.AddDays(i))
            .Select(dia => new ResumenDia(
                dia,
                reservasDia.GetValueOrDefault(dia),
                cancelacionesDia.GetValueOrDefault(dia)))
            .ToList();

        var porCategoria = await copiasDeLibrosActivos
            .GroupBy(e => e.Libro.Categoria)
            .OrderBy(g => g.Key)
            .Select(g => new ResumenCategoria(
                g.Key,
                g.Count(e => e.Estado == EstadoEjemplar.Disponible),
                g.Count(e => e.Estado == EstadoEjemplar.Reservado),
                g.Count(e => e.Estado == EstadoEjemplar.Inactivo)))
            .ToListAsync(cancellationToken);

        var masReservadosIds = await db.Reservas.AsNoTracking()
            .Where(r => r.FechaSolicitud >= inicioVentanaUtc)
            .GroupBy(r => r.Ejemplar.IdLibro)
            .Select(g => new { IdLibro = g.Key, Reservas = g.Count() })
            .OrderByDescending(x => x.Reservas)
            .ThenBy(x => x.IdLibro)
            .Take(TopMasReservados)
            .ToListAsync(cancellationToken);
        var idsTop = masReservadosIds.Select(x => x.IdLibro).ToList();
        var librosTop = await db.Libros.AsNoTracking()
            .Where(l => idsTop.Contains(l.IdLibro))
            .Select(l => new { l.IdLibro, l.Isbn, l.Titulo, l.Autor })
            .ToDictionaryAsync(l => l.IdLibro, cancellationToken);
        var masReservados = masReservadosIds
            .Select(x => new ResumenLibroReservado(
                x.IdLibro, librosTop[x.IdLibro].Isbn, librosTop[x.IdLibro].Titulo, librosTop[x.IdLibro].Autor, PortadaUrl: null, x.Reservas))
            .ToList();

        var porAgotarse = await db.Libros.AsNoTracking()
            .Where(l => l.Estado == EstadoLibro.Activo)
            .Select(l => new
            {
                l.IdLibro,
                l.Isbn,
                l.Titulo,
                Disponibles = l.Ejemplares.Count(e => e.Estado == EstadoEjemplar.Disponible),
                Total = l.Ejemplares.Count(e => e.Estado != EstadoEjemplar.Inactivo)
            })
            .Where(x => x.Disponibles <= 1)
            .OrderBy(x => x.Disponibles)
            .ThenBy(x => x.Titulo)
            .Take(TopPorAgotarse)
            .Select(x => new ResumenLibroPorAgotarse(x.IdLibro, x.Isbn, x.Titulo, x.Disponibles, x.Total))
            .ToListAsync(cancellationToken);

        var ultimasReservas = await db.Reservas.AsNoTracking()
            .OrderByDescending(r => r.FechaSolicitud)
            .Take(TopActividad)
            .Select(r => new ResumenActividad(
                "reserva", r.Lector.Nombre, r.Ejemplar.Libro.Titulo, r.Ejemplar.CodigoInventario, r.FechaSolicitud))
            .ToListAsync(cancellationToken);
        var ultimasCancelaciones = await db.Reservas.AsNoTracking()
            .Where(r => r.FechaCancelacion != null)
            .OrderByDescending(r => r.FechaCancelacion)
            .Take(TopActividad)
            .Select(r => new ResumenActividad(
                "cancelacion", r.Lector.Nombre, r.Ejemplar.Libro.Titulo, r.Ejemplar.CodigoInventario, r.FechaCancelacion!.Value))
            .ToListAsync(cancellationToken);
        var actividad = ultimasReservas.Concat(ultimasCancelaciones)
            .OrderByDescending(a => a.Fecha)
            .Take(TopActividad)
            .ToList();

        return Result.Success(new ObtenerResumenResponse(kpis, serie, porCategoria, masReservados, porAgotarse, actividad));
    }
}
