using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace backend_asa.Common.OpenApi;

/// <summary>
/// La API serializa los enums con JsonStringEnumConverter. Este transformer declara su schema como
/// "type": "string" para que los clientes generados desde OpenAPI los tipen como texto.
/// </summary>
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
