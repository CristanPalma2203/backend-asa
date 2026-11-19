using backend_asa.Domain.Enums;

namespace backend_asa.Domain;

public class Lector
{
    public int IdLector { get; set; }
    public string Identificador { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public EstadoLector Estado { get; set; }
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
