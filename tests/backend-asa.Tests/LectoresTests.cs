using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Tests;

public sealed class LectoresTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Crear_CorreoDuplicado_Devuelve409()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var response = await admin.PostAsJsonAsync("/api/lectores", new
        {
            identificador = "LEC-DUP-01",
            nombre = "Otra persona",
            correo = AnaCorreo
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Codigo(response)).Should().Be("correo-duplicado");
    }

    [Fact]
    public async Task Desactivar_ConReservasActivas_Devuelve422()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var lectorId = await IdLector(AnaCorreo);
        var libroId = await IdLibro("9788498381498");
        var reserva = await admin.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);
        reserva.StatusCode.Should().Be(HttpStatusCode.Created);

        var baja = await admin.DeleteAsync($"/api/lectores/{lectorId}");

        baja.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(baja)).Should().Be("lector-con-reservas-activas");
    }

    [Fact]
    public async Task Crear_ConRolLector_Devuelve403()
    {
        var ana = await LoginAsync(AnaCorreo, AnaPassword);
        var response = await ana.PostAsJsonAsync("/api/lectores", new
        {
            identificador = "LEC-FORB-01",
            nombre = "No admin",
            correo = "no.admin@demo.local"
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Codigo(response)).Should().Be("acceso-denegado");
    }

    [Fact]
    public async Task Crear_ConPassword_PermiteLogin()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        const string correo = "nuevo.lector@demo.local";
        const string password = "Nuevo-demo-123";
        var alta = await admin.PostAsJsonAsync("/api/lectores", new
        {
            identificador = "LEC-NUEVO-01",
            nombre = "Nuevo Lector",
            correo,
            password
        }, Json);

        alta.StatusCode.Should().Be(HttpStatusCode.Created);
        alta.Headers.Location.Should().NotBeNull();

        var login = await Client.PostAsJsonAsync("/api/auth/login", new { correo, password }, Json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LeerJson(login)).GetProperty("rol").GetString().Should().Be("Lector");
    }

    private async Task<int> IdLector(string correo) =>
        await Fixture.EnDb(db => db.Lectores
            .Where(lector => lector.Correo == correo)
            .Select(lector => lector.IdLector)
            .SingleAsync());

    private async Task<int> IdLibro(string isbn)
    {
        var response = await Client.GetAsync("/api/libros?page=1&pageSize=50");
        var json = await LeerJson(response);
        return json.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("isbn").GetString() == isbn)
            .GetProperty("idLibro")
            .GetInt32();
    }
}
