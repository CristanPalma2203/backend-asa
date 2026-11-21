namespace backend_asa.Features.Lector.Crear;

public sealed record CrearLectorRequest(string Identificador, string Nombre, string Correo, string? Password);
