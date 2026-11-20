using backend_asa.Domain;
using backend_asa.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend_asa.Infrastructure.Persistence.Configurations;

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.HasKey(r => r.IdReserva);

        builder.HasOne(r => r.Lector)
            .WithMany(l => l.Reservas)
            .HasForeignKey(r => r.IdLector)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.IdLector);

        builder.HasOne(r => r.Ejemplar)
            .WithMany(e => e.Reservas)
            .HasForeignKey(r => r.IdEjemplar)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.IdEjemplar, "IX_Reservas_IdEjemplar");

        builder.HasIndex(r => r.IdEjemplar, "IX_Reservas_IdEjemplar_Activa")
            .IsUnique()
            .HasFilter("[Estado] = 'Activa'");

        builder.Property(r => r.FechaSolicitud)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(r => r.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(20)");

        builder.HasIndex(r => r.Estado);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Reserva_Estado",
                $"[Estado] IN ('{nameof(EstadoReserva.Activa)}', '{nameof(EstadoReserva.Cancelada)}', '{nameof(EstadoReserva.Completada)}')");

            t.HasCheckConstraint(
                "CK_Reserva_FechaCancelacion",
                "([Estado] <> 'Cancelada') OR ([FechaCancelacion] IS NOT NULL)");
        });
    }
}
