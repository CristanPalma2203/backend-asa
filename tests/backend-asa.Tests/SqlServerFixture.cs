using backend_asa.Infrastructure.Persistence;
using backend_asa.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace backend_asa.Tests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer _container = null!;
    private Respawner _respawner = null!;

    public string ConnectionString { get; private set; } = string.Empty;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _container.StartAsync();
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "BibliotecaAsa"
        };
        ConnectionString = builder.ConnectionString;

        // Valores solo de test: los tests no dependen de appsettings.Development.json.
        // Las contraseñas coinciden con IntegrationTest.AdminPassword / AnaPassword.
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:database-asa"] = ConnectionString,
            ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
            ["Jwt:Key"] = "clave-jwt-solo-para-tests-de-integracion-0123456789",
            ["Seed:AdminPassword"] = "Admin-demo-123",
            ["Seed:LectorPassword"] = "Ana-demo-123"
        };

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            // Program.cs lee Jwt:Key y Seed:* de builder.Configuration antes de Build();
            // UseSetting llega a esa configuración temprana y AddInMemoryCollection a la final.
            foreach (var (key, value) in settings)
            {
                host.UseSetting(key, value);
            }

            host.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
        });

        Client = Factory.CreateClient();

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
        await db.Database.MigrateAsync();

        await using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                TablesToIgnore = [new Table("__EFMigrationsHistory")]
            });
        }
    }

    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await DatosIniciales.SeedAsync(
            db,
            config["Seed:AdminPassword"],
            config["Seed:LectorPassword"],
            CancellationToken.None);
    }

    public async Task<T> EnDb<T>(Func<BibliotecaDbContext, Task<T>> accion)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
        return await accion(db);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Nombre)]
public sealed class SqlCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Nombre = "sql";
}
