using System.Net;
using System.Net.Http.Json;
using backend_asa.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Tests;

public sealed class EjemplaresTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Crear_CodigoDuplicado_Devuelve409()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var libroId = await IdLibro("9788498381498");
        var response = await admin.PostAsJsonAsync($"/api/libros/{libroId}/ejemplares", new
        {
            codigoInventario = "BIB-001"
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Codigo(response)).Should().Be("codigo-inventario-duplicado");
    }

    [Fact]
    public async Task CambiarEstado_EjemplarReservado_Devuelve422()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var libroId = await IdLibro("9788498381498");
        var lectorId = await IdLector("ana.lopez@demo.local");
        var reserva = await admin.PostAsJsonAsync("/api/reservas", new
        {
            lectorId,
            libroId
        }, Json);
        reserva.StatusCode.Should().Be(HttpStatusCode.Created);
        var creada = await LeerJson(reserva);
        var ejemplarId = creada.GetProperty("idEjemplar").GetInt32();

        var ejemplares = await admin.GetAsync($"/api/libros/{libroId}/ejemplares");
        var lista = await LeerJson(ejemplares);
        var rowVersion = lista.EnumerateArray()
            .Single(item => item.GetProperty("idEjemplar").GetInt32() == ejemplarId)
            .GetProperty("rowVersion")
            .GetString();

        var cambio = await admin.PutAsJsonAsync(
            $"/api/libros/{libroId}/ejemplares/{ejemplarId}/estado",
            new { estado = "Inactivo", rowVersion },
            Json);

        cambio.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(cambio)).Should().Be("ejemplar-reservado");
    }

    [Fact]
    public async Task Editar_ConRowVersionVieja_Devuelve409YConservaElPrimerCambio()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var libroId = await IdLibro("9788498381498");
        var (ejemplarId, rowVersion) = await PrimerEjemplar(admin, libroId);
        var primera = await admin.PutAsJsonAsync(
            $"/api/ejemplares/{ejemplarId}",
            new { codigoInventario = "BIB-900", rowVersion },
            Json);
        primera.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var segunda = await admin.PutAsJsonAsync(
            $"/api/ejemplares/{ejemplarId}",
            new { codigoInventario = "BIB-901", rowVersion },
            Json);

        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Codigo(segunda)).Should().Be("conflicto-concurrencia");
        var codigo = await Fixture.EnDb(db => db.Ejemplares
            .Where(ejemplar => ejemplar.IdEjemplar == ejemplarId)
            .Select(ejemplar => ejemplar.CodigoInventario)
            .SingleAsync());
        codigo.Should().Be("BIB-900");
    }

    [Fact]
    public async Task Desactivar_EjemplarDisponible_QuedaInactivoSinBorrarse()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var libroId = await IdLibro("9788498381498");
        var (ejemplarId, _) = await PrimerEjemplar(admin, libroId);

        var response = await admin.DeleteAsync($"/api/ejemplares/{ejemplarId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var estado = await Fixture.EnDb(db => db.Ejemplares
            .Where(ejemplar => ejemplar.IdEjemplar == ejemplarId)
            .Select(ejemplar => (EstadoEjemplar?)ejemplar.Estado)
            .SingleOrDefaultAsync());
        estado.Should().Be(EstadoEjemplar.Inactivo);
        var disponibles = await Fixture.EnDb(db => db.Ejemplares.CountAsync(ejemplar =>
            ejemplar.IdLibro == libroId && ejemplar.Estado == EstadoEjemplar.Disponible));
        disponibles.Should().Be(2);
    }

    private async Task<(int IdEjemplar, string RowVersion)> PrimerEjemplar(HttpClient admin, int libroId)
    {
        var response = await admin.GetAsync($"/api/libros/{libroId}/ejemplares");
        var primero = (await LeerJson(response)).EnumerateArray().First();
        return (primero.GetProperty("idEjemplar").GetInt32(), primero.GetProperty("rowVersion").GetString()!);
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
}
