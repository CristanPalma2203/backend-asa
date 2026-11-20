using backend_asa.Domain.Enums;

namespace backend_asa.Domain;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public EstadoUsuario Estado { get; set; }
    public int? IdLector { get; set; }
    public DateTime CreatedAt { get; set; }
    public Lector? Lector { get; set; }
}
