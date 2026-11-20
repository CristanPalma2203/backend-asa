
namespace backend_asa.Features.Auth.Login;

public sealed record AuthSession(string? AccessToken, DateTimeOffset? ExpiresAt, string Rol, string Nombre, int? LectorId);
