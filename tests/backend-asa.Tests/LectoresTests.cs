using System.Net;
using System.Net.Http.Json;
using backend_asa.Domain.Enums;
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
    public async Task Activar_LectorDesactivado_LoReactivaConSuUsuarioYEsIdempotente()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var lectorId = await IdLector(AnaCorreo);
        (await admin.DeleteAsync($"/api/lectores/{lectorId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var loginBloqueado = await Client.PostAsJsonAsync("/api/auth/login", new { correo = AnaCorreo, password = AnaPassword }, Json);
        loginBloqueado.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var activacion = await admin.PostAsync($"/api/lectores/{lectorId}/activar", null);
        var otraVez = await admin.PostAsync($"/api/lectores/{lectorId}/activar", null);

        activacion.StatusCode.Should().Be(HttpStatusCode.NoContent);
        otraVez.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var estados = await Fixture.EnDb(db => db.Lectores
            .Where(lector => lector.IdLector == lectorId)
            .Select(lector => new
            {
                Lector = lector.Estado,
                Usuario = db.Usuarios.Where(usuario => usuario.IdLector == lectorId).Select(usuario => usuario.Estado).Single()
            })
            .SingleAsync());
        estados.Lector.Should().Be(EstadoLector.Activo);
        estados.Usuario.Should().Be(EstadoUsuario.Activo);
        var ana = await LoginAsync(AnaCorreo, AnaPassword);
        (await ana.GetAsync($"/api/lectores/{lectorId}/reservas")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Activar_NoExiste_Devuelve404_YComoLector_Devuelve403()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var ana = await LoginAsync(AnaCorreo, AnaPassword);
        var lectorId = await IdLector(AnaCorreo);

        var inexistente = await admin.PostAsync("/api/lectores/999999/activar", null);
        var comoLector = await ana.PostAsync($"/api/lectores/{lectorId}/activar", null);

        inexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Codigo(inexistente)).Should().Be("lector-no-encontrado");
        comoLector.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Codigo(comoLector)).Should().Be("acceso-denegado");
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
