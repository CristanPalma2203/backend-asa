using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Tests;

public sealed class ContratoTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    private const string PrincipitoIsbn = "9788498381498";

    [Theory]
    [InlineData("/health")]              // respuesta normal
    [InlineData("/api/libros/999999")]   // 404 ProblemDetails
    [InlineData("/api/lectores")]        // 401 sin token
    public async Task Respuestas_TraenHeadersDeSeguridad(string url)
    {
        var response = await Client.GetAsync(url);

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("no-referrer");
    }

    [Fact]
    public async Task Body_JsonRoto_Devuelve400JsonInvalidoComoProblemDetails()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        using var body = new StringContent("{ \"isbn\": \"9780000000002\", \"titulo\": ", System.Text.Encoding.UTF8, "application/json");

        var response = await admin.PostAsync("/api/libros", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await LeerJson(response);
        problem.GetProperty("code").GetString().Should().Be("json-invalido");
        problem.GetProperty("status").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task Reserva_FechaSolicitudSaleEnUtcConZ_AlCrearLeerYListar()
    {
        var libroId = await IdLibro(PrincipitoIsbn);
        var lectorId = await IdLector(AnaCorreo);
        var ana = await LoginAsync(AnaCorreo, AnaPassword);

        var alta = await ana.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);
        alta.StatusCode.Should().Be(HttpStatusCode.Created);
        var creada = await LeerJson(alta);
        DebeSerUtc(creada.GetProperty("fechaSolicitud"));
        creada.TryGetProperty("fechaReserva", out _).Should().BeFalse();

        var idReserva = creada.GetProperty("idReserva").GetInt32();
        var detalle = await LeerJson(await ana.GetAsync($"/api/reservas/{idReserva}"));
        DebeSerUtc(detalle.GetProperty("fechaSolicitud"));
        detalle.TryGetProperty("fechaReserva", out _).Should().BeFalse();
        detalle.GetProperty("estado").GetString().Should().Be("Activa");

        var cancelacion = await ana.PostAsync($"/api/reservas/{idReserva}/cancelar", null);
        cancelacion.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lista = await LeerJson(await ana.GetAsync($"/api/lectores/{lectorId}/reservas"));
        var item = lista.GetProperty("items").EnumerateArray()
            .Single(reserva => reserva.GetProperty("idReserva").GetInt32() == idReserva);
        DebeSerUtc(item.GetProperty("fechaSolicitud"));
        DebeSerUtc(item.GetProperty("fechaCancelacion"));
        item.GetProperty("estado").GetString().Should().Be("Cancelada");
    }

    [Fact]
    public async Task Libro_DetalleYListadoDeEjemplares_UsanIdEjemplarYEstadoComoTexto()
    {
        var libroId = await IdLibro(PrincipitoIsbn);

        var detalle = await LeerJson(await Client.GetAsync($"/api/libros/{libroId}"));
        detalle.GetProperty("estado").GetString().Should().Be("Activo");
        var ejemplares = detalle.GetProperty("ejemplares").EnumerateArray().ToList();
        ejemplares.Should().NotBeEmpty();
        foreach (var ejemplar in ejemplares)
        {
            ejemplar.TryGetProperty("idEjemplar", out _).Should().BeTrue();
            ejemplar.TryGetProperty("id", out _).Should().BeFalse();
            ejemplar.GetProperty("estado").ValueKind.Should().Be(JsonValueKind.String);
        }

        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var lista = await LeerJson(await admin.GetAsync($"/api/libros/{libroId}/ejemplares"));
        lista.EnumerateArray().Select(item => item.GetProperty("idEjemplar").GetInt32())
            .Should().BeEquivalentTo(ejemplares.Select(item => item.GetProperty("idEjemplar").GetInt32()));
    }

    [Fact]
    public async Task OpenApi_PublicaLosEnumsComoString()
    {
        var response = await Client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var schemas = (await LeerJson(response)).GetProperty("components").GetProperty("schemas");

        var estadoReserva = schemas.GetProperty("EstadoReserva");
        estadoReserva.GetProperty("type").GetString().Should().Be("string");
        estadoReserva.GetProperty("enum").EnumerateArray().Select(valor => valor.GetString())
            .Should().Equal("Activa", "Cancelada", "Completada");

        foreach (var nombre in new[] { "EstadoEjemplar", "EstadoLibro", "EstadoLector" })
        {
            var schema = schemas.GetProperty(nombre);
            schema.GetProperty("type").GetString().Should().Be("string", nombre);
            schema.GetProperty("enum").EnumerateArray()
                .Should().OnlyContain(valor => valor.ValueKind == JsonValueKind.String, nombre);
        }
    }

    [Theory]
    [InlineData("corta")]
    [InlineData("1234567")]
    public async Task CrearLector_PasswordMenorA8_Devuelve400(string password)
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var response = await admin.PostAsJsonAsync("/api/lectores", new
        {
            identificador = "LEC-PWD-01",
            nombre = "Clave Corta",
            correo = "clave.corta@demo.local",
            password
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Codigo(response)).Should().Be("validacion-fallida");
        var creado = await Fixture.EnDb(db => db.Lectores.AnyAsync(lector => lector.Identificador == "LEC-PWD-01"));
        creado.Should().BeFalse();
    }

    [Fact]
    public async Task CrearLector_SinPassword_CreaLectorSinUsuario()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var response = await admin.PostAsJsonAsync("/api/lectores", new
        {
            identificador = "LEC-SINPWD-01",
            nombre = "Sin Clave",
            correo = "sin.clave@demo.local"
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var creado = await LeerJson(response);
        creado.GetProperty("tieneUsuario").GetBoolean().Should().BeFalse();
        creado.GetProperty("estado").GetString().Should().Be("Activo");
    }

    private static void DebeSerUtc(JsonElement fecha)
    {
        var texto = fecha.GetString();
        texto.Should().NotBeNull().And.EndWith("Z");
        DateTimeOffset.Parse(texto!).Offset.Should().Be(TimeSpan.Zero);
    }

    private async Task<int> IdLibro(string isbn)
    {
        var json = await LeerJson(await Client.GetAsync("/api/libros?page=1&pageSize=50"));
        return json.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("isbn").GetString() == isbn)
            .GetProperty("idLibro")
            .GetInt32();
    }

    private async Task<int> IdLector(string correo) =>
        await Fixture.EnDb(db => db.Lectores
            .Where(lector => lector.Correo == correo)
            .Select(lector => lector.IdLector)
            .SingleAsync());
}
