using backend_asa.Domain.Enums;

namespace backend_asa.Features.Lector.Obtener;

public sealed record ObtenerLectorResponse(
    int IdLector,
    string Identificador,
    string Nombre,
    string Correo,
    EstadoLector Estado,
    bool TieneUsuario,
    int ReservasActivas);
