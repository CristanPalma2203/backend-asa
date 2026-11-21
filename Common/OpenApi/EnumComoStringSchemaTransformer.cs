using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace backend_asa.Common.OpenApi;

public sealed class EnumComoStringSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var tipo = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (tipo.IsEnum)
        {
            schema.Type = schema.Type is { } actual && actual.HasFlag(JsonSchemaType.Null)
                ? JsonSchemaType.String | JsonSchemaType.Null
                : JsonSchemaType.String;
        }

        return Task.CompletedTask;
    }
}
