using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend_asa.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Tests;

public sealed class ResumenTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    private const string PrincipitoIsbn = "9788498381498";

    [Fact]
    public async Task Obtener_ComoAdmin_DevuelveTodasLasSeccionesConSuForma()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var lectorId = await IdLector(AnaCorreo);
        var libroId = await IdLibro(PrincipitoIsbn);
        (await admin.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await admin.GetAsync("/api/resumen");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await LeerJson(response);

        var kpis = json.GetProperty("kpis");
        foreach (var nombre in new[] { "librosActivos", "ejemplaresActivos", "ejemplaresDisponibles", "ejemplaresReservados", "lectoresActivos", "reservasActivas" })
            kpis.GetProperty(nombre).ValueKind.Should().Be(JsonValueKind.Number, nombre);
        kpis.GetProperty("librosActivos").GetInt32().Should().Be(25);
        kpis.GetProperty("ejemplaresReservados").GetInt32().Should().Be(1);
        kpis.GetProperty("reservasActivas").GetInt32().Should().Be(1);
        var ocupacion = kpis.GetProperty("ocupacion").GetDouble();
        ocupacion.Should().BeApproximately(1.0 / kpis.GetProperty("ejemplaresActivos").GetInt32(), 0.0001);

        json.GetProperty("serieDiaria").GetArrayLength().Should().Be(14);

        var categoria = json.GetProperty("porCategoria").EnumerateArray().First();
        categoria.GetProperty("categoria").ValueKind.Should().Be(JsonValueKind.String);
        foreach (var nombre in new[] { "disponibles", "reservados", "inactivos" })
            categoria.GetProperty(nombre).ValueKind.Should().Be(JsonValueKind.Number, nombre);

        var top = json.GetProperty("masReservados").EnumerateArray().ToList();
        top.Should().ContainSingle();
        top[0].GetProperty("libroId").GetInt32().Should().Be(libroId);
        top[0].GetProperty("isbn").GetString().Should().Be(PrincipitoIsbn);
        top[0].GetProperty("titulo").GetString().Should().Be("El Principito");
        top[0].GetProperty("autor").ValueKind.Should().Be(JsonValueKind.String);
        top[0].GetProperty("portadaUrl").ValueKind.Should().Be(JsonValueKind.Null);
        top[0].GetProperty("reservas").GetInt32().Should().Be(1);

        json.GetProperty("porAgotarse").ValueKind.Should().Be(JsonValueKind.Array);

        var evento = json.GetProperty("actividad").EnumerateArray().Should().ContainSingle().Subject;
        evento.GetProperty("tipo").GetString().Should().Be("reserva");
        evento.GetProperty("lector").GetString().Should().Be("Ana López");
        evento.GetProperty("libro").GetString().Should().Be("El Principito");
        evento.GetProperty("codigoEjemplar").GetString().Should().NotBeNullOrEmpty();
        evento.GetProperty("fecha").GetString().Should().EndWith("Z");
    }

    [Theory]
    [InlineData(14)]
    [InlineData(30)]
    public async Task Obtener_SerieDiaria_RellenaLosDiasSinDatosConCero(int dias)
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var ana = await LoginAsync(AnaCorreo, AnaPassword);
        var lectorId = await IdLector(AnaCorreo);
        var alta = await LeerJson(await ana.PostAsJsonAsync("/api/reservas", new { lectorId, libroId = await IdLibro(PrincipitoIsbn) }, Json));
        (await ana.PostAsync($"/api/reservas/{alta.GetProperty("idReserva").GetInt32()}/cancelar", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var json = await LeerJson(await admin.GetAsync($"/api/resumen?dias={dias}"));

        var serie = json.GetProperty("serieDiaria").EnumerateArray().ToList();
        serie.Should().HaveCount(dias);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var fechas = serie.Select(d => DateOnly.Parse(d.GetProperty("fecha").GetString()!)).ToList();
        fechas.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        fechas[0].Should().Be(hoy.AddDays(-(dias - 1)));
        fechas[^1].Should().Be(hoy);

        serie[^1].GetProperty("reservas").GetInt32().Should().Be(1);
        serie[^1].GetProperty("cancelaciones").GetInt32().Should().Be(1);
        serie.Take(dias - 1).Should().OnlyContain(d =>
            d.GetProperty("reservas").GetInt32() == 0 && d.GetProperty("cancelaciones").GetInt32() == 0);

        json.GetProperty("actividad").EnumerateArray().Select(a => a.GetProperty("tipo").GetString())
            .Should().Equal("cancelacion", "reserva");
    }

    [Fact]
    public async Task Obtener_DiasInvalido_Devuelve400()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);

        var response = await admin.GetAsync("/api/resumen?dias=7");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Codigo(response)).Should().Be("validacion-fallida");
    }

    [Fact]
    public async Task Obtener_ComoLector_Devuelve403_YSinToken401()
    {
        var ana = await LoginAsync(AnaCorreo, AnaPassword);

        var comoLector = await ana.GetAsync("/api/resumen");
        var anonimo = await Client.GetAsync("/api/resumen");

        comoLector.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Codigo(comoLector)).Should().Be("acceso-denegado");
        anonimo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Obtener_PorAgotarse_IncluyeLibroSinDisponibles_PrimeroYSinContarInactivos()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var lectorId = await IdLector(AnaCorreo);
        var libroId = await IdLibro(PrincipitoIsbn);
        (await admin.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var copias = await LeerJson(await admin.GetAsync($"/api/libros/{libroId}/ejemplares"));
        foreach (var copia in copias.EnumerateArray().Where(c => c.GetProperty("estado").GetString() == "Disponible"))
            (await admin.DeleteAsync($"/api/ejemplares/{copia.GetProperty("idEjemplar").GetInt32()}"))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var json = await LeerJson(await admin.GetAsync("/api/resumen"));

        var lista = json.GetProperty("porAgotarse").EnumerateArray().ToList();
        var principito = lista.Single(l => l.GetProperty("libroId").GetInt32() == libroId);
        principito.GetProperty("isbn").GetString().Should().Be(PrincipitoIsbn);
        principito.GetProperty("titulo").GetString().Should().Be("El Principito");
        principito.GetProperty("disponibles").GetInt32().Should().Be(0);
        principito.GetProperty("total").GetInt32().Should().Be(1);
        lista.Select(l => l.GetProperty("disponibles").GetInt32()).Should().BeInAscendingOrder()
            .And.OnlyContain(d => d <= 1);
        lista[0].GetProperty("disponibles").GetInt32().Should().Be(0);

        var kpis = json.GetProperty("kpis");
        kpis.GetProperty("ejemplaresReservados").GetInt32().Should().Be(1);
        json.GetProperty("porCategoria").EnumerateArray().Sum(c => c.GetProperty("inactivos").GetInt32())
            .Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Obtener_PorAgotarse_DevuelveComoMaximo8()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        await Fixture.EnDb(db => db.Ejemplares.ExecuteUpdateAsync(
            set => set.SetProperty(e => e.Estado, EstadoEjemplar.Inactivo)));

        var json = await LeerJson(await admin.GetAsync("/api/resumen"));

        var lista = json.GetProperty("porAgotarse").EnumerateArray().ToList();
        lista.Should().HaveCount(8);
        lista.Should().OnlyContain(l => l.GetProperty("disponibles").GetInt32() == 0
            && !string.IsNullOrEmpty(l.GetProperty("isbn").GetString()));
    }

    private async Task<int> IdLector(string correo) =>
        await Fixture.EnDb(db => db.Lectores
            .Where(lector => lector.Correo == correo)
            .Select(lector => lector.IdLector)
            .SingleAsync());

    private async Task<int> IdLibro(string isbn)
    {
        var json = await LeerJson(await Client.GetAsync("/api/libros?page=1&pageSize=50"));
        return json.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("isbn").GetString() == isbn)
            .GetProperty("idLibro")
            .GetInt32();
    }
}
