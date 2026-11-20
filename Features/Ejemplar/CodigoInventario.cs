using FluentValidation;

namespace backend_asa.Features.Ejemplar;

internal static class CodigoInventario
{
    public static string Normalizar(string? codigo) =>
        string.IsNullOrWhiteSpace(codigo)
            ? string.Empty
            : string.Concat(codigo.Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();
}
