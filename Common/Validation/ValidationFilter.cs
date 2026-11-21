using FluentValidation;

namespace backend_asa.Common.Validation;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context);
        }

        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var validacion = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (validacion.IsValid)
        {
            return await next(context);
        }

        var errors = validacion.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return TypedResults.ValidationProblem(
            errors,
            title: "Solicitud no válida",
            detail: "Revisa los datos enviados.",
            extensions: new Dictionary<string, object?> { ["code"] = "validacion-fallida" });
    }
}
