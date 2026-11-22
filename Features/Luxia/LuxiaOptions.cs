namespace backend_asa.Features.Luxia;

public sealed class LuxiaOptions
{
    public const string SectionName = "Luxia";
    public const string PoliticaPorUsuario = "luxia-por-usuario";

    public string BaseUrl { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public string? ApiKey { get; init; }
    public TimeSpan TiempoMaximo { get; init; } = TimeSpan.FromSeconds(15);
    public int PreguntasPorMinuto { get; init; } = 10;
}
