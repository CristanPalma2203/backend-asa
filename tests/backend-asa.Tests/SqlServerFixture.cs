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

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("ConnectionStrings:database-asa", ConnectionString);
            host.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:database-asa"] = ConnectionString,
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
                });
            });
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
