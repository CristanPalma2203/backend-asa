using backend_asa.Domain.Enums;

namespace backend_asa.Features.Lector.Listar;

public sealed record ListarLectoresRequest(string? Buscar, EstadoLector? Estado, int Page = 1, int PageSize = 10);
