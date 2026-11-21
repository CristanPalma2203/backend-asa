using backend_asa.Domain;
using backend_asa.Domain.Enums;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Tests;

public sealed class LimpiezaDatosPruebaTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Script_ConConfirmar_BorraSoloPruebaYRestauraDisponibles()
    {
        await Fixture.EnDb(async db =>
        {
            var ahora = DateTime.UtcNow;
            var qa = new Libro
            {
                Isbn = "9780000000099",
                Titulo = "QA API Live Libro 2",
                Autor = "Autor Prueba",
                Categoria = "Novela",
                Estado = EstadoLibro.Activo
            };
            var seQueda = new Libro
            {
                Isbn = "9780000000088",
                Titulo = "Apuntes de sala",
                Autor = "Archivo",
                Categoria = "Notas",
                Estado = EstadoLibro.Activo
            };
            db.AddRange(qa, seQueda);
            db.Add(new Ejemplar
            {
                Libro = qa,
                CodigoInventario = "QA-LIVE-01",
                Estado = EstadoEjemplar.Disponible,
                CreatedAt = ahora,
                UpdatedAt = ahora
            });
            db.Add(new Ejemplar
            {
                Libro = seQueda,
                CodigoInventario = "SALA-01",
                Estado = EstadoEjemplar.Disponible,
                CreatedAt = ahora,
                UpdatedAt = ahora
            });

            var copiaSeed = await db.Ejemplares
                .Where(e => e.Libro.Isbn == "9788498381498" && e.Estado == EstadoEjemplar.Disponible)
                .OrderBy(e => e.CodigoInventario)
                .FirstAsync();
            copiaSeed.Estado = EstadoEjemplar.Reservado;
            copiaSeed.UpdatedAt = ahora;

            var lector = new Lector
            {
                Identificador = "QA-LIVE-01",
                Nombre = "Lector de Prueba",
                Correo = "qa.live@test.local",
                Estado = EstadoLector.Activo
            };
            db.Add(lector);
            db.Add(new Usuario
            {
                Correo = "qa.live@test.local",
                Nombre = "Lector de Prueba",
                PasswordHash = "hash-de-prueba",
                Rol = RolUsuario.Lector,
                Estado = EstadoUsuario.Activo,
                Lector = lector,
                CreatedAt = ahora
            });
            db.Add(new Reserva
            {
                Lector = lector,
                Ejemplar = copiaSeed,
                FechaSolicitud = ahora,
                Estado = EstadoReserva.Activa
            });
            await db.SaveChangesAsync();
            return 0;
        });

        var preview = await Ejecutar(confirmar: false);
        preview.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["Ejemplares"] = 1,
            ["Lectores"] = 1,
            ["Libros"] = 1,
            ["Reservas"] = 1,
            ["Usuarios"] = 1
        });

        (await Contar("Libros", "Titulo = N'QA API Live Libro 2'")).Should().Be(1);

        await Ejecutar(confirmar: true);

        (await Contar("Libros")).Should().Be(26);
        (await Contar("Ejemplares")).Should().Be(52);
        (await Contar("Lectores")).Should().Be(2);
        (await Contar("Usuarios")).Should().Be(2);
        (await Contar("Reservas")).Should().Be(0);
        (await Contar("Libros", "Titulo = N'QA API Live Libro 2'")).Should().Be(0);
        (await Contar("Libros", "Titulo = N'Apuntes de sala'")).Should().Be(1);
        (await Contar("Usuarios", "Correo = 'admin@demo.local'")).Should().Be(1);
        (await Contar("Usuarios", "Correo = 'ana.lopez@demo.local'")).Should().Be(1);
        (await Contar("Ejemplares", "CodigoInventario LIKE 'BIB-%' AND Estado = 'Disponible' AND IdLibro IN (SELECT IdLibro FROM Libros WHERE Isbn = '9788498381498')")).Should().Be(3);
        (await Contar("Ejemplares", "CodigoInventario IN ('BIB-018', 'BIB-019') AND Estado = 'Inactivo'")).Should().Be(2);
    }

    private async Task<Dictionary<string, int>> Ejecutar(bool confirmar)
    {
        var sql = await File.ReadAllTextAsync(RutaScript());
        if (confirmar)
        {
            sql = sql.Replace("DECLARE @Confirmar bit = 0;", "DECLARE @Confirmar bit = 1;", StringComparison.Ordinal);
        }

        var conteo = new Dictionary<string, int>();
        await using var connection = new SqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            conteo[reader.GetString(0)] = reader.GetInt32(1);
        }

        while (await reader.NextResultAsync())
        {
        }

        return conteo;
    }

    private async Task<int> Contar(string tabla, string? where = null)
    {
        await using var connection = new SqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();
        var sql = where is null
            ? $"SELECT COUNT(*) FROM {tabla}"
            : $"SELECT COUNT(*) FROM {tabla} WHERE {where}";
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static string RutaScript()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "scripts", "limpieza-datos-prueba.sql")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir!.FullName, "scripts", "limpieza-datos-prueba.sql");
    }
}
