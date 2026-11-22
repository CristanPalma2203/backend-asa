namespace backend_asa.Features.Luxia.Preguntar;

public sealed record PreguntarLuxiaResponse(string Texto, IReadOnlyList<int> IdsLibros);
