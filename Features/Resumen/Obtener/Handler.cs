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
        // Ventana en UTC: hoy y los (dias - 1) días anteriores, desde las 00:00.
        var hoy = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var primerDia = hoy.AddDays(-(request.Dias - 1));
        var desde = primerDia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Copias de libros activos. Las inactivas no cuentan como activas.
        var copias = db.Ejemplares.AsNoTracking().Where(e => e.Libro.Estado == EstadoLibro.Activo);

        var activos = await copias.CountAsync(e => e.Estado != EstadoEjemplar.Inactivo, cancellationToken);
        var disponibles = await copias.CountAsync(e => e.Estado == EstadoEjemplar.Disponible, cancellationToken);
        var reservados = await copias.CountAsync(e => e.Estado == EstadoEjemplar.Reservado, cancellationToken);

        var kpis = new ResumenKpis(
            await db.Libros.AsNoTracking().CountAsync(l => l.Estado == EstadoLibro.Activo, cancellationToken),
            activos,
            disponibles,
            reservados,
            await db.Lectores.AsNoTracking().CountAsync(l => l.Estado == EstadoLector.Activo, cancellationToken),
            await db.Reservas.AsNoTracking().CountAsync(r => r.Estado == EstadoReserva.Activa, cancellationToken),
            activos == 0 ? 0 : Math.Round((double)reservados / activos, 4));

        // Serie: SQL agrupa por día; los días sin datos se rellenan con 0 aquí.
        var reservasPorDia = await db.Reservas.AsNoTracking()
            .Where(r => r.FechaSolicitud >= desde)
            .GroupBy(r => r.FechaSolicitud.Date)
            .Select(g => new { Dia = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);
        var cancelacionesPorDia = await db.Reservas.AsNoTracking()
            .Where(r => r.FechaCancelacion != null && r.FechaCancelacion >= desde)
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

        var porCategoria = await copias
            .GroupBy(e => e.Libro.Categoria)
            .OrderBy(g => g.Key)
            .Select(g => new ResumenCategoria(
                g.Key,
                g.Count(e => e.Estado == EstadoEjemplar.Disponible),
                g.Count(e => e.Estado == EstadoEjemplar.Reservado),
                g.Count(e => e.Estado == EstadoEjemplar.Inactivo)))
            .ToListAsync(cancellationToken);

        var top = await db.Reservas.AsNoTracking()
            .Where(r => r.FechaSolicitud >= desde)
            .GroupBy(r => r.Ejemplar.IdLibro)
            .Select(g => new { IdLibro = g.Key, Reservas = g.Count() })
            .OrderByDescending(x => x.Reservas)
            .ThenBy(x => x.IdLibro)
            .Take(TopMasReservados)
            .ToListAsync(cancellationToken);
        var idsTop = top.Select(x => x.IdLibro).ToList();
        var librosTop = await db.Libros.AsNoTracking()
            .Where(l => idsTop.Contains(l.IdLibro))
            .Select(l => new { l.IdLibro, l.Isbn, l.Titulo, l.Autor })
            .ToDictionaryAsync(l => l.IdLibro, cancellationToken);
        // El modelo todavía no guarda portada: PortadaUrl sale en null.
        var masReservados = top
            .Select(x => new ResumenLibroReservado(
                x.IdLibro, librosTop[x.IdLibro].Isbn, librosTop[x.IdLibro].Titulo, librosTop[x.IdLibro].Autor, null, x.Reservas))
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

        // Actividad: las 10 últimas de cada tipo, mezcladas en memoria.
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
