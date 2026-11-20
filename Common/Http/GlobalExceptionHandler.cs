using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Common.Http;

public sealed class GlobalExceptionHandler(IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, code, title, detail) = Map(exception);

        var problem = TypedResults.Problem(
            detail: detail,
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = code });

        await problem.ExecuteAsync(httpContext);
        return true;
    }

    private (int Status, string Code, string Title, string Detail) Map(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            return (
                StatusCodes.Status409Conflict,
                "conflicto-concurrencia",
                "Conflicto",
                "El registro fue modificado por otro usuario. Vuelve a cargar los datos e inténtalo de nuevo.");
        }

        if (exception is DbUpdateException { InnerException: SqlException sql } && sql.Number is 2601 or 2627)
        {
            return (
                StatusCodes.Status409Conflict,
                "registro-duplicado",
                "Conflicto",
                "Ya existe un registro con esos datos.");
        }

        var detail = environment.IsDevelopment()
            ? exception.Message
            : "Ocurrió un error interno. Inténtalo de nuevo más tarde.";

        return (
            StatusCodes.Status500InternalServerError,
            "error-interno",
            "Error interno",
            detail);
    }
}
