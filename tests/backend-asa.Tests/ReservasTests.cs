using System.Net;
using System.Net.Http.Json;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Tests;

public sealed class ReservasTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Crear_ElPrincipito_BajaDisponiblesYMarcaLaCopiaReservada()
    {
        var libroId = await IdLibro("9788498381498");
        var lectorId = await IdLector(AnaCorreo);
        var ana = await LoginAsync(AnaCorreo, AnaPassword);

        var reserva = await ana.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);
        reserva.StatusCode.Should().Be(HttpStatusCode.Created);
        var creada = await LeerJson(reserva);
        var ejemplarId = creada.GetProperty("idEjemplar").GetInt32();
        creada.GetProperty("estado").GetString().Should().Be("Activa");

        (await Disponibles(libroId)).Should().Be(2);
        (await EstadoEjemplarDe(libroId, ejemplarId)).Should().Be((int)EstadoEjemplar.Reservado);
    }

    [Fact]
    public async Task Cancelar_ReservaActiva_DevuelveLaCopiaYElStock()
    {
        var libroId = await IdLibro("9788498381498");
        var lectorId = await IdLector(AnaCorreo);
        var ana = await LoginAsync(AnaCorreo, AnaPassword);
        var reserva = await ana.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);
        var creada = await LeerJson(reserva);
        var idReserva = creada.GetProperty("idReserva").GetInt32();
        var ejemplarId = creada.GetProperty("idEjemplar").GetInt32();

        var cancelacion = await ana.PostAsync($"/api/reservas/{idReserva}/cancelar", null);

        cancelacion.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Disponibles(libroId)).Should().Be(3);
        (await EstadoEjemplarDe(libroId, ejemplarId)).Should().Be((int)EstadoEjemplar.Disponible);
    }

    [Fact]
    public async Task Crear_LectorInactivo_Devuelve422()
    {
        var libroId = await IdLibro("9788498381498");
        var lectorId = await IdLector("carlos.mendez@demo.local");
        var admin = await LoginAsync(AdminCorreo, AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(response)).Should().Be("lector-inactivo");
    }

    [Fact]
    public async Task Crear_SinStock_Devuelve409YNoGuardaReserva()
    {
        var libroId = await IdLibro("9788420412146");
        var lectorId = await IdLector(AnaCorreo);
        var ana = await LoginAsync(AnaCorreo, AnaPassword);

        var response = await ana.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Codigo(response)).Should().Be("sin-stock");
        var reservas = await Fixture.EnDb(db => db.Reservas.CountAsync(reserva => reserva.Ejemplar.IdLibro == libroId));
        reservas.Should().Be(0);
    }

    [Fact]
    public async Task Crear_ReservaDuplicadaDelMismoLibro_Devuelve422()
    {
        var libroId = await IdLibro("9788498381498");
        var lectorId = await IdLector(AnaCorreo);
        var ana = await LoginAsync(AnaCorreo, AnaPassword);
        var primera = await ana.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);
        primera.StatusCode.Should().Be(HttpStatusCode.Created);

        var segunda = await ana.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);

        segunda.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(segunda)).Should().Be("reserva-duplicada");
    }

    [Fact]
    public async Task Crear_SinToken_Devuelve401()
    {
        var response = await Client.PostAsJsonAsync("/api/reservas", new { lectorId = 1, libroId = 1 }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Codigo(response)).Should().Be("no-autenticado");
    }

    [Fact]
    public async Task Crear_LectorReservaPorOtro_Devuelve403()
    {
        var libroId = await IdLibro("9788498381498");
        var otroId = await IdLector("carlos.mendez@demo.local");
        var ana = await LoginAsync(AnaCorreo, AnaPassword);

        var response = await ana.PostAsJsonAsync("/api/reservas", new { lectorId = otroId, libroId }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Codigo(response)).Should().Be("acceso-denegado");
    }

    [Fact]
    public async Task Crear_DosReservasSimultaneasDeLaUnicaCopia_UnaGanaYQuedaUnaActiva()
    {
        var libroId = await IdLibro("9788420471839");
        (await Disponibles(libroId)).Should().Be(1);
        var anaId = await IdLector(AnaCorreo);
        var segundoId = await Fixture.EnDb(async db =>
        {
            var lector = new Lector
            {
                Identificador = "LEC-TEST-002",
                Nombre = "Luis Pérez",
                Correo = "luis.perez@demo.local",
                Estado = EstadoLector.Activo
            };
            db.Lectores.Add(lector);
            await db.SaveChangesAsync();
            return lector.IdLector;
        });

        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        using var barrera = new Barrier(2);
        var primera = ReservarAlMismoTiempo(admin, anaId, libroId, barrera);
        var segunda = ReservarAlMismoTiempo(admin, segundoId, libroId, barrera);
        var respuestas = await Task.WhenAll(primera, segunda);

        respuestas.Count(respuesta => respuesta.StatusCode == HttpStatusCode.Created).Should().Be(1);
        respuestas.Count(respuesta => respuesta.StatusCode == HttpStatusCode.Conflict).Should().Be(1);

        var creada = respuestas.Single(respuesta => respuesta.StatusCode == HttpStatusCode.Created);
        var ejemplarId = (await LeerJson(creada)).GetProperty("idEjemplar").GetInt32();
        var activas = await Fixture.EnDb(db => db.Reservas.CountAsync(reserva =>
            reserva.IdEjemplar == ejemplarId && reserva.Estado == EstadoReserva.Activa));
        activas.Should().Be(1);
    }

    private async Task<HttpResponseMessage> ReservarAlMismoTiempo(HttpClient cliente, int lectorId, int libroId, Barrier barrera)
    {
        barrera.SignalAndWait(TimeSpan.FromSeconds(30));
        return await cliente.PostAsJsonAsync("/api/reservas", new { lectorId, libroId }, Json);
    }

    private async Task<int> IdLibro(string isbn)
    {
        var response = await Client.GetAsync("/api/libros?page=1&pageSize=50");
        var json = await LeerJson(response);
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

    private async Task<int> Disponibles(int libroId)
    {
        var response = await Client.GetAsync("/api/libros?page=1&pageSize=50");
        var json = await LeerJson(response);
        return json.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("idLibro").GetInt32() == libroId)
            .GetProperty("disponibles")
            .GetInt32();
    }

    private async Task<int> EstadoEjemplarDe(int libroId, int ejemplarId)
    {
        var estado = await Fixture.EnDb(db => db.Ejemplares
            .Where(ejemplar => ejemplar.IdEjemplar == ejemplarId && ejemplar.IdLibro == libroId)
            .Select(ejemplar => ejemplar.Estado)
            .SingleAsync());
        return (int)estado;
    }
}
