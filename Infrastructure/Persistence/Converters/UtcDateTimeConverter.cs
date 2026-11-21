using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace backend_asa.Infrastructure.Persistence.Converters;

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
