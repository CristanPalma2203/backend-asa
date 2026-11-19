using backend_asa.Domain.Enums;

namespace backend_asa.Domain;

public class Libro
{
    public int IdLibro { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public EstadoLibro Estado { get; set; }
    public ICollection<Ejemplar> Ejemplares { get; set; } = new List<Ejemplar>();
}
