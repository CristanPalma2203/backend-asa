using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace backend_asa.Tests;

public sealed class LibrosTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Listar_ConDatosIniciales_Devuelve25YElPrincipitoCon3Disponibles()
    {
        var response = await Client.GetAsync("/api/libros?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await LeerJson(response);
        json.GetProperty("totalCount").GetInt32().Should().Be(25);

        var principito = json.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("isbn").GetString() == "9788498381498");
        principito.GetProperty("titulo").GetString().Should().Be("El Principito");
        principito.GetProperty("disponibles").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Crear_IsbnDuplicado_Devuelve409IsbnDuplicado()
    {
        var response = await Client.PostAsJsonAsync("/api/libros", new
        {
            isbn = "9788498381498",
            titulo = "Otro título",
            autor = "Otro autor",
            categoria = "Novela"
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Codigo(response)).Should().Be("isbn-duplicado");
    }

    [Fact]
    public async Task Crear_IsbnInvalido_Devuelve400()
    {
        var response = await Client.PostAsJsonAsync("/api/libros", new
        {
            isbn = "123",
            titulo = "Título",
            autor = "Autor",
            categoria = "Novela"
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Codigo(response)).Should().Be("validacion-fallida");
    }
}
