using System.Net;
using System.Net.Http.Json;
using backend_asa.Domain;
using backend_asa.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;

namespace backend_asa.Tests;

public sealed class UsuariosTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Login_UsuarioInactivo_Devuelve401()
    {
        var hasher = new PasswordHasher<Usuario>();
        var usuario = new Usuario
        {
            Correo = "inactivo@demo.local",
            Nombre = "Inactivo",
            Rol = RolUsuario.Lector,
            Estado = EstadoUsuario.Inactivo,
            CreatedAt = DateTime.UtcNow
        };
        usuario.PasswordHash = hasher.HashPassword(usuario, "Inactivo-demo-123");

        await Fixture.EnDb(async db =>
        {
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            return 0;
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            correo = "inactivo@demo.local",
            password = "Inactivo-demo-123"
        }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Codigo(response)).Should().Be("credenciales-invalidas");
    }
}
