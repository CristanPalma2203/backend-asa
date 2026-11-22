using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.Extensions.Options;

namespace backend_asa.Features.Luxia;

public sealed class MoonshotClient(HttpClient http, IOptions<LuxiaOptions> opciones)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public async Task<ChatMensaje?> CompletarAsync(
        IReadOnlyList<ChatMensaje> mensajes,
        IReadOnlyList<ChatHerramienta> herramientas,
        CancellationToken cancellationToken)
    {
        var config = opciones.Value;
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(
                new ChatSolicitud(config.Modelo, mensajes, herramientas, Temperature: 0.3),
                options: Json)
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        using var respuesta = await http.SendAsync(solicitud, cancellationToken);
        respuesta.EnsureSuccessStatusCode();

        var completado = await respuesta.Content.ReadFromJsonAsync<ChatCompletado>(Json, cancellationToken);
        return completado?.Choices is [var eleccion, ..] ? eleccion.Message : null;
    }
}

public sealed record ChatMensaje(
    string Role,
    string? Content,
    IReadOnlyList<ChatLlamadaHerramienta>? ToolCalls = null,
    string? ToolCallId = null);

public sealed record ChatLlamadaHerramienta(string Id, string Type, ChatFuncionLlamada Function);

public sealed record ChatFuncionLlamada(string Name, string Arguments);

public sealed record ChatHerramienta(string Type, ChatFuncion Function);

public sealed record ChatFuncion(string Name, string Description, JsonElement Parameters);

public sealed record ChatSolicitud(
    string Model,
    IReadOnlyList<ChatMensaje> Messages,
    IReadOnlyList<ChatHerramienta> Tools,
    double Temperature);

public sealed record ChatCompletado(IReadOnlyList<ChatEleccion> Choices);

public sealed record ChatEleccion(ChatMensaje Message);
