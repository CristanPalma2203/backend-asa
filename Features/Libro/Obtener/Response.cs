using backend_asa.Domain.Enums;

namespace backend_asa.Features.Libro.Obtener;

public sealed record ObtenerLibroEjemplar(int IdEjemplar, string CodigoInventario, EstadoEjemplar Estado, string RowVersion);

public sealed record ObtenerLibroResponse(
    int IdLibro,
    string Isbn,
    string Titulo,
    string Autor,
    string Categoria,
    EstadoLibro Estado,
    int TotalEjemplares,
    int Disponibles,
    IReadOnlyList<ObtenerLibroEjemplar> Ejemplares);
