namespace backend_asa.Features.Auth.Me;

public sealed record MeResponse(string? AccessToken, DateTimeOffset? ExpiresAt, string Rol, string Nombre, int? LectorId);
