namespace backend_asa.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;

    public string Key { get; set; } = string.Empty;
}

public sealed class DemoUserOptions
{
    public const string SectionName = "DemoUsers";

    public string Correo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;
}
