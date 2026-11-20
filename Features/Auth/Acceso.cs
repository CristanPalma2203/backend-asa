using System.Security.Claims;
using backend_asa.Common.Results;

namespace backend_asa.Features.Auth;

public static class Acceso
{
    public const string LectorIdClaim = "lector_id";
    public const string RolAdmin = "Admin";
    public const string RolLector = "Lector";

    public static bool EsAdmin(ClaimsPrincipal usuario) => usuario.IsInRole(RolAdmin);

    public static int? LectorId(ClaimsPrincipal usuario) =>
        int.TryParse(usuario.FindFirst(LectorIdClaim)?.Value, out var id) ? id : null;

    public static Result<int> ResolverLector(ClaimsPrincipal usuario, int lectorIdSolicitado)
    {
        if (EsAdmin(usuario))
        {
            return Result.Success(lectorIdSolicitado);
        }

        var propio = LectorId(usuario);
        if (propio is null || propio != lectorIdSolicitado)
        {
            return Result.Failure<int>(Denegado("No puedes reservar a nombre de otro lector."));
        }

        return Result.Success(propio.Value);
    }

    public static Result? DenegarSiNoEsDuenoOAdmin(ClaimsPrincipal usuario, int lectorId)
    {
        if (EsAdmin(usuario))
        {
            return null;
        }

        var propio = LectorId(usuario);
        if (propio == lectorId)
        {
            return null;
        }

        return Result.Failure(Denegado("No puedes acceder a los datos de otro lector."));
    }

    private static Error Denegado(string mensaje) =>
        new("acceso-denegado", mensaje, ErrorType.Forbidden);
}
