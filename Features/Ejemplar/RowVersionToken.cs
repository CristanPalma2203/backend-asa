namespace backend_asa.Features.Ejemplar;

internal static class RowVersionToken
{
    private const int RowVersionBytes = 8;

    public static bool EsValido(string? token) => TryRead(token, out _);

    public static bool TryRead(string? token, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var buffer = new byte[RowVersionBytes];
        if (!Convert.TryFromBase64String(token, buffer, out var escritos) || escritos != RowVersionBytes)
        {
            return false;
        }

        bytes = buffer;
        return true;
    }
}
