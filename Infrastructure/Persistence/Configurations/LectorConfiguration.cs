using backend_asa.Domain;
using backend_asa.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_asa.Infrastructure.Persistence.Configurations;

public class LectorConfiguration : IEntityTypeConfiguration<Lector>
{
    public void Configure(EntityTypeBuilder<Lector> builder)
    {
        builder.HasKey(l => l.IdLector);

        builder.Property(l => l.Identificador)
            .IsRequired()
            .HasColumnType("varchar(20)");

        builder.HasIndex(l => l.Identificador)
            .IsUnique();

        builder.Property(l => l.Nombre)
            .IsRequired()
            .HasColumnType("nvarchar(150)");

        builder.Property(l => l.Correo)
            .IsRequired()
            .HasColumnType("varchar(150)");

        builder.HasIndex(l => l.Correo)
            .IsUnique();

        builder.Property(l => l.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(20)");

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Lector_Estado",
                $"[Estado] IN ('{nameof(EstadoLector.Activo)}', '{nameof(EstadoLector.Inactivo)}')"));
    }
}
