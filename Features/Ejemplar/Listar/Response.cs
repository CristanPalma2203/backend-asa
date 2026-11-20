using backend_asa.Domain.Enums;

namespace backend_asa.Features.Ejemplar.Listar;

public sealed record ListarEjemplaresItem(int IdEjemplar, string CodigoInventario, EstadoEjemplar Estado, string RowVersion);
