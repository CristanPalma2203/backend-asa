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
    public async Task Listar_ConSearch_FuncionaComoBuscarEnTituloAutorEIsbn()
    {
        var porIsbn = await Titulos("/api/libros?search=9780062&page=1&pageSize=50");
        var porAutor = await Titulos("/api/libros?search=harari&page=1&pageSize=50");
        var porTitulo = await Titulos("/api/libros?search=hobbit&page=1&pageSize=50");

        porIsbn.Should().Equal("Homo Deus", "Sapiens");
        porAutor.Should().Equal("Homo Deus", "Sapiens");
        porTitulo.Should().Equal("El hobbit");
        porIsbn.Should().Equal(await Titulos("/api/libros?buscar=9780062&page=1&pageSize=50"));
    }

    [Fact]
    public async Task Listar_ConAutor_FiltraPorAutorSinDistinguirMayusculas()
    {
        var harari = await Titulos("/api/libros?autor=HARARI&page=1&pageSize=50");
        var porTitulo = await Titulos("/api/libros?autor=hobbit&page=1&pageSize=50");

        harari.Should().Equal("Homo Deus", "Sapiens");
        porTitulo.Should().BeEmpty();
    }

    [Fact]
    public async Task Crear_IsbnDuplicado_Devuelve409IsbnDuplicado()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var response = await admin.PostAsJsonAsync("/api/libros", new
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
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var response = await admin.PostAsJsonAsync("/api/libros", new
        {
            isbn = "123",
            titulo = "Título",
            autor = "Autor",
            categoria = "Novela"
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Codigo(response)).Should().Be("validacion-fallida");
    }

    [Fact]
    public async Task Crear_ComoAdmin_Devuelve201()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var response = await admin.PostAsJsonAsync("/api/libros", new
        {
            isbn = "9780134494166",
            titulo = "Clean Architecture",
            autor = "Robert C. Martin",
            categoria = "Tecnología"
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task TotalEjemplares_CuentaSoloLosActivos_EnListadoYDetalle()
    {
        var admin = await LoginAsync(AdminCorreo, AdminPassword);
        var listado = await LeerJson(await Client.GetAsync("/api/libros?page=1&pageSize=50"));
        var libroId = listado.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("isbn").GetString() == "9788498381498")
            .GetProperty("idLibro").GetInt32();
        var copias = await LeerJson(await admin.GetAsync($"/api/libros/{libroId}/ejemplares"));
        var ejemplarId = copias.EnumerateArray().First().GetProperty("idEjemplar").GetInt32();
        (await admin.DeleteAsync($"/api/ejemplares/{ejemplarId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var item = (await LeerJson(await Client.GetAsync("/api/libros?page=1&pageSize=50")))
            .GetProperty("items").EnumerateArray()
            .Single(libro => libro.GetProperty("idLibro").GetInt32() == libroId);
        var detalle = await LeerJson(await Client.GetAsync($"/api/libros/{libroId}"));

        item.GetProperty("totalEjemplares").GetInt32().Should().Be(2);
        item.GetProperty("disponibles").GetInt32().Should().Be(2);
        detalle.GetProperty("totalEjemplares").GetInt32().Should().Be(2);
        detalle.GetProperty("disponibles").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Listar_BuscarPorNombreDeCategoria_DevuelveLosLibrosDeEsaCategoria()
    {
        var novelas = await Titulos("/api/libros?buscar=novela&page=1&pageSize=50");
        var clasicosSinAcento = await Titulos("/api/libros?search=CLASICOS&page=1&pageSize=50");

        novelas.Should().BeEquivalentTo(
            "Cien años de soledad", "Pedro Páramo", "Rayuela", "La casa de los espíritus",
            "Matar a un ruiseñor", "1984", "Fahrenheit 451");
        clasicosSinAcento.Should().BeEquivalentTo(
            "Don Quijote de la Mancha", "Orgullo y prejuicio", "Crimen y castigo",
            "La Odisea", "Hamlet", "El hobbit");
    }

    [Fact]
    public async Task Listar_CategoriaIdConTexto_CombinaAmbosFiltros()
    {
        var soloTexto = await Titulos("/api/libros?buscar=de&page=1&pageSize=50");
        var textoEnInfantil = await Titulos("/api/libros?buscar=de&categoriaId=Infantil&page=1&pageSize=50");
        var soloInfantil = await Titulos("/api/libros?categoriaId=Infantil&page=1&pageSize=50");

        soloTexto.Should().Contain(["Cien años de soledad", "Don Quijote de la Mancha"]);
        textoEnInfantil.Should().Equal("Charlie y la fábrica de chocolate", "El Principito");
        soloInfantil.Should().Equal(
            "Charlie y la fábrica de chocolate", "El Principito", "Harry Potter y la piedra filosofal");
    }

    [Fact]
    public async Task Listar_CategoriaIdDemasiadoLargo_Devuelve400()
    {
        var response = await Client.GetAsync($"/api/libros?categoriaId={new string('x', 81)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Codigo(response)).Should().Be("validacion-fallida");
    }

    private async Task<List<string>> Titulos(string url)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await LeerJson(response);
        return json.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("titulo").GetString()!)
            .ToList();
    }
}
