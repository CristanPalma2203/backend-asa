using backend_asa.Domain;
using backend_asa.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_asa.Infrastructure.Persistence.Configurations;

public class LibroConfiguration : IEntityTypeConfiguration<Libro>
{
    public void Configure(EntityTypeBuilder<Libro> builder)
    {
        builder.HasKey(l => l.IdLibro);

        builder.Property(l => l.Isbn)
            .IsRequired()
            .HasColumnType("varchar(20)");

        builder.HasIndex(l => l.Isbn)
            .IsUnique();

        builder.Property(l => l.Titulo)
            .IsRequired()
            .HasColumnType("nvarchar(200)");

        builder.Property(l => l.Autor)
            .IsRequired()
            .HasColumnType("nvarchar(150)");

        builder.Property(l => l.Categoria)
            .IsRequired()
            .HasColumnType("nvarchar(80)");

        builder.Property(l => l.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(20)");

        builder.HasIndex(l => l.Estado);

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Libro_Estado",
                $"[Estado] IN ('{nameof(EstadoLibro.Activo)}', '{nameof(EstadoLibro.Inactivo)}')"));
    }
}
