using backend_asa.Domain.Enums;

namespace backend_asa.Domain;

public class Ejemplar
{
    public int IdEjemplar { get; set; }
    public int IdLibro { get; set; }
    public string CodigoInventario { get; set; } = string.Empty;
    public EstadoEjemplar Estado { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Libro Libro { get; set; } = null!;
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
