using backend_asa.Domain.Enums;

namespace backend_asa.Features.Ejemplar.CambiarEstado;

public sealed record CambiarEstadoEjemplarRequest(EstadoEjemplar Estado, string RowVersion);
