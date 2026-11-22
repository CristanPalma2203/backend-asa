using backend_asa.Common.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace backend_asa.Common.Http;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? TypedResults.NoContent()
            : Problem(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : Problem(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result, string createdUri) =>
        result.IsSuccess
            ? TypedResults.Created(createdUri, result.Value)
            : Problem(result.Error);

    private static ProblemHttpResult Problem(Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Solicitud no válida"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "No encontrado"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflicto"),
            ErrorType.BusinessRule => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio no satisfecha"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "No autenticado"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Acceso denegado"),
            ErrorType.ServiceUnavailable => (StatusCodes.Status503ServiceUnavailable, "Servicio no disponible"),
            _ => (StatusCodes.Status400BadRequest, "Solicitud no válida")
        };

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
