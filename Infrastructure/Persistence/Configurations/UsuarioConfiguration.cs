using backend_asa.Domain;
using backend_asa.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_asa.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios", t =>
        {
            t.HasCheckConstraint(
                "CK_Usuario_Rol",
                $"[Rol] IN ('{nameof(RolUsuario.Admin)}', '{nameof(RolUsuario.Lector)}')");

            t.HasCheckConstraint(
                "CK_Usuario_Estado",
                $"[Estado] IN ('{nameof(EstadoUsuario.Activo)}', '{nameof(EstadoUsuario.Inactivo)}')");
        });

        builder.HasKey(u => u.IdUsuario);

        builder.Property(u => u.Correo)
            .IsRequired()
            .HasColumnType("varchar(150)");

        builder.HasIndex(u => u.Correo)
            .IsUnique();

        builder.Property(u => u.Nombre)
            .IsRequired()
            .HasColumnType("nvarchar(150)");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasColumnType("nvarchar(500)");

        builder.Property(u => u.Rol)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(20)");

        builder.Property(u => u.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(20)");

        builder.HasOne(u => u.Lector)
            .WithMany()
            .HasForeignKey(u => u.IdLector)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.IdLector)
            .IsUnique()
            .HasFilter("[IdLector] IS NOT NULL");

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
