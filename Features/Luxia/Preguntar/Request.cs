namespace backend_asa.Features.Luxia.Preguntar;

public sealed record PreguntarLuxiaRequest(string Pregunta, IReadOnlyList<MensajeHistorial>? Historial);

public sealed record MensajeHistorial(string Rol, string Texto)
{
    public const string RolUsuario = "usuario";
    public const string RolLuxia = "luxia";
}
