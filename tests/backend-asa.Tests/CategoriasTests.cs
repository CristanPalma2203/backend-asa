using System.Net;
using backend_asa.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Tests;

public sealed class CategoriasTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Listar_DevuelveCategoriasConLibrosActivosOrdenadasPorNombre()
    {
        var response = await Client.GetAsync("/api/categorias");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categorias = (await LeerJson(response)).EnumerateArray()
            .Select(item => (
                Id: item.GetProperty("id").GetString(),
                Nombre: item.GetProperty("nombre").GetString(),
                CantidadLibros: item.GetProperty("cantidadLibros").GetInt32()))
            .ToList();
        categorias.Should().Equal(
            ("Ciencia", "Ciencia", 3),
            ("Clásicos", "Clásicos", 6),
            ("Historia", "Historia", 3),
            ("Infantil", "Infantil", 3),
            ("Novela", "Novela", 7),
            ("Tecnología", "Tecnología", 3));
    }

    [Fact]
    public async Task Listar_CategoriaSinLibrosActivos_NoAparece()
    {
        await Fixture.EnDb(db => db.Libros
            .Where(libro => libro.Categoria == "Tecnología")
            .ExecuteUpdateAsync(libro => libro.SetProperty(l => l.Estado, EstadoLibro.Inactivo)));
        await Fixture.EnDb(db => db.Libros
            .Where(libro => libro.Titulo == "Sapiens")
            .ExecuteUpdateAsync(libro => libro.SetProperty(l => l.Estado, EstadoLibro.Inactivo)));

        var response = await Client.GetAsync("/api/categorias");

        var categorias = (await LeerJson(response)).EnumerateArray()
            .ToDictionary(item => item.GetProperty("nombre").GetString()!, item => item.GetProperty("cantidadLibros").GetInt32());
        categorias.Should().NotContainKey("Tecnología");
        categorias["Historia"].Should().Be(2);
    }
}
