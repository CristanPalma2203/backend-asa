namespace backend_asa.Features.Resumen.Obtener;

public sealed record ObtenerResumenResponse(
    ResumenKpis Kpis,
    IReadOnlyList<ResumenDia> SerieDiaria,
    IReadOnlyList<ResumenCategoria> PorCategoria,
    IReadOnlyList<ResumenLibroReservado> MasReservados,
    IReadOnlyList<ResumenLibroPorAgotarse> PorAgotarse,
    IReadOnlyList<ResumenActividad> Actividad);

public sealed record ResumenKpis(
    int LibrosActivos,
    int EjemplaresActivos,
    int EjemplaresDisponibles,
    int EjemplaresReservados,
    int LectoresActivos,
    int ReservasActivas,
    double Ocupacion);

public sealed record ResumenDia(DateOnly Fecha, int Reservas, int Cancelaciones);

public sealed record ResumenCategoria(string Categoria, int Disponibles, int Reservados, int Inactivos);

public sealed record ResumenLibroReservado(int LibroId, string Isbn, string Titulo, string Autor, string? PortadaUrl, int Reservas);

public sealed record ResumenLibroPorAgotarse(int LibroId, string Isbn, string Titulo, int Disponibles, int Total);

public sealed record ResumenActividad(string Tipo, string Lector, string Libro, string CodigoEjemplar, DateTime Fecha);
