using backend_asa.Domain;
using backend_asa.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_asa.Infrastructure.Persistence.Configurations;

public class EjemplarConfiguration : IEntityTypeConfiguration<Ejemplar>
{
    public void Configure(EntityTypeBuilder<Ejemplar> builder)
    {
        builder.HasKey(e => e.IdEjemplar);

        builder.HasOne(e => e.Libro)
            .WithMany(l => l.Ejemplares)
            .HasForeignKey(e => e.IdLibro)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CodigoInventario)
            .IsRequired()
            .HasColumnType("varchar(30)");

        builder.HasIndex(e => e.CodigoInventario)
            .IsUnique();

        builder.Property(e => e.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(20)");

        builder.HasIndex(e => e.Estado);

        // Cubre también las búsquedas solo por IdLibro (columna inicial) y la FK hacia Libros,
        // por eso no hay un índice aparte sobre IdLibro.
        builder.HasIndex(e => new { e.IdLibro, e.Estado });

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Ejemplar_Estado",
                $"[Estado] IN ('{nameof(EstadoEjemplar.Disponible)}', '{nameof(EstadoEjemplar.Reservado)}', '{nameof(EstadoEjemplar.Prestado)}', '{nameof(EstadoEjemplar.Inactivo)}')"));
    }
}
