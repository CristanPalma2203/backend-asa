using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace backend_asa.Infrastructure.Persistence.Converters;

/// <summary>
/// SQL Server guarda datetime2 sin zona. Al escribir normaliza a UTC y al leer marca el valor como Utc,
/// para que la API serialice siempre ISO 8601 con "Z".
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    valor => ToUtc(valor),
    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc))
{
    private static DateTime ToUtc(DateTime valor) => valor.Kind switch
    {
        DateTimeKind.Utc => valor,
        DateTimeKind.Local => valor.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
    };
}
