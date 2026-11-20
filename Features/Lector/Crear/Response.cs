using backend_asa.Domain.Enums;

namespace backend_asa.Features.Lector.Crear;

public sealed record CrearLectorResponse(
    int IdLector,
    string Identificador,
    string Nombre,
    string Correo,
    EstadoLector Estado,
    bool TieneUsuario);
