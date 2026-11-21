namespace backend_asa.Features.Ejemplar;

internal static class RowVersionToken
{
    public static bool EsValido(string? token) => TryRead(token, out _);

    public static bool TryRead(string? token, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(token);
        }
        catch (FormatException)
        {
            return false;
        }

        return bytes.Length == 8;
    }
}
