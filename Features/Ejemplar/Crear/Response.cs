using backend_asa.Domain.Enums;

namespace backend_asa.Features.Ejemplar.Crear;

public sealed record CrearEjemplarResponse(int IdEjemplar, string CodigoInventario, EstadoEjemplar Estado, string RowVersion);
