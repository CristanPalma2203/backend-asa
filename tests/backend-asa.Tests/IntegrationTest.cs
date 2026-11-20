using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace backend_asa.Tests;

[Collection(SqlCollection.Nombre)]
public abstract class IntegrationTest(SqlServerFixture fixture) : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected SqlServerFixture Fixture { get; } = fixture;

    protected HttpClient Client => Fixture.Client;

    protected const string AdminCorreo = "admin@demo.local";
    protected const string AdminPassword = "Admin-demo-123";
    protected const string AnaCorreo = "ana.lopez@demo.local";
    protected const string AnaPassword = "Ana-demo-123";

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

    protected async Task<HttpClient> LoginAsync(string correo, string password)
    {
        var client = Fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { correo, password }, Json);
        response.EnsureSuccessStatusCode();
        var json = await LeerJson(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        return client;
    }
}
