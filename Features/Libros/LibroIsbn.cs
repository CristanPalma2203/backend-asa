namespace backend_asa.Features.Libros;

internal static class LibroIsbn
{
    public static bool EsValido(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return false;
        }

        var limpio = Limpiar(isbn);
        return limpio.Length switch
        {
            10 => EsIsbn10(limpio),
            13 => EsIsbn13(limpio),
            _ => false
        };
    }

    public static string Limpiar(string isbn)
    {
        Span<char> buffer = stackalloc char[isbn.Length];
        var length = 0;
        foreach (var character in isbn)
        {
            if (char.IsDigit(character))
            {
                buffer[length++] = character;
            }
            else if (character is 'X' or 'x')
            {
                buffer[length++] = 'X';
            }
        }

        return new string(buffer[..length]);
    }

    private static bool EsIsbn10(string isbn)
    {
        var suma = 0;
        for (var i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return false;
            }

            suma += (isbn[i] - '0') * (10 - i);
        }

        var verificador = isbn[9] == 'X'
            ? 10
            : char.IsDigit(isbn[9]) ? isbn[9] - '0' : -1;

        if (verificador < 0)
        {
            return false;
        }

        return (suma + verificador) % 11 == 0;
    }

    private static bool EsIsbn13(string isbn)
    {
        foreach (var character in isbn)
        {
            if (!char.IsDigit(character))
            {
                return false;
            }
        }

        var suma = 0;
        for (var i = 0; i < 12; i++)
        {
            suma += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        var verificador = (10 - (suma % 10)) % 10;
        return verificador == isbn[12] - '0';
    }
}
