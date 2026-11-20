using backend_asa.Domain;
using backend_asa.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace backend_asa.Infrastructure.Persistence;

public class BibliotecaDbContext : DbContext
{
    public BibliotecaDbContext(DbContextOptions<BibliotecaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Ejemplar> Ejemplares => Set<Ejemplar>();
    public DbSet<Lector> Lectores => Set<Lector>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BibliotecaDbContext).Assembly);
    }
}
