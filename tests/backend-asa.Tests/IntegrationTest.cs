using System.Net.Http.Json;
using System.Text.Json;

namespace backend_asa.Tests;

[Collection(SqlCollection.Nombre)]
public abstract class IntegrationTest(SqlServerFixture fixture) : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected SqlServerFixture Fixture { get; } = fixture;

    protected HttpClient Client => Fixture.Client;

    public Task InitializeAsync() => Fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected static async Task<JsonElement> LeerJson(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return json;
    }

    protected static async Task<string?> Codigo(HttpResponseMessage response)
    {
        var json = await LeerJson(response);
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
