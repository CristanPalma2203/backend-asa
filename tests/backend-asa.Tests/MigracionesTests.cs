using backend_asa.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Tests;

public sealed class MigracionesTests(SqlServerFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Modelo_SinCambiosPendientes_NoTieneMigracionesPorAplicar()
    {
        var pendiente = await Fixture.EnDb(db => Task.FromResult(db.Database.HasPendingModelChanges()));
        pendiente.Should().BeFalse();
    }
}
