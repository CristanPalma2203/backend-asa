using backend_asa.Domain.Enums;

namespace backend_asa.Domain;

public class Reserva
{
    public int IdReserva { get; set; }
    public int IdLector { get; set; }
    public int IdEjemplar { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public EstadoReserva Estado { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public Lector Lector { get; set; } = null!;
    public Ejemplar Ejemplar { get; set; } = null!;
}
