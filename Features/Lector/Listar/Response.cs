using backend_asa.Domain.Enums;

namespace backend_asa.Features.Lector.Listar;

public sealed record ListarLectoresItem(
    int IdLector,
    string Identificador,
    string Nombre,
    string Correo,
    EstadoLector Estado,
    bool TieneUsuario);
