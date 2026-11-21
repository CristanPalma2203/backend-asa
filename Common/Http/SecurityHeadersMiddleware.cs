namespace backend_asa.Common.Http;

/// <summary>
/// Agrega headers de seguridad básicos a todas las respuestas, incluidas las de error.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        // OnStarting: se aplican justo antes de enviar la respuesta, así sobreviven aunque
        // UseExceptionHandler limpie los headers al reescribir una respuesta de error.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        });

        return next(context);
    }
}
