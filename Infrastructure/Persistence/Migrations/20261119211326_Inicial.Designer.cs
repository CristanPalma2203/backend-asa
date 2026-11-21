using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using backend_asa.Infrastructure.Persistence;

#nullable disable

namespace backend_asa.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(BibliotecaDbContext))]
    [Migration("20261119211326_Inicial")]
    partial class Inicial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("backend_asa.Domain.Ejemplar", b =>
                {
                    b.Property<int>("IdEjemplar")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdEjemplar"));

                    b.Property<string>("CodigoInventario")
                        .IsRequired()
                        .HasColumnType("varchar(30)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<int>("IdLibro")
                        .HasColumnType("int");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.HasKey("IdEjemplar");

                    b.HasIndex("CodigoInventario")
                        .IsUnique();

                    b.HasIndex("Estado");

                    b.HasIndex("IdLibro");

                    b.HasIndex("IdLibro", "Estado");

                    b.ToTable("Ejemplares", t =>
                        {
                            t.HasCheckConstraint("CK_Ejemplar_Estado", "[Estado] IN ('Disponible', 'Reservado', 'Prestado', 'Inactivo')");
                        });
                });

            modelBuilder.Entity("backend_asa.Domain.Lector", b =>
                {
                    b.Property<int>("IdLector")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdLector"));

                    b.Property<string>("Correo")
                        .IsRequired()
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Identificador")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasColumnType("nvarchar(150)");

                    b.HasKey("IdLector");

                    b.HasIndex("Correo")
                        .IsUnique();

                    b.HasIndex("Identificador")
                        .IsUnique();

                    b.ToTable("Lectores", t =>
                        {
                            t.HasCheckConstraint("CK_Lector_Estado", "[Estado] IN ('Activo', 'Inactivo')");
                        });
                });

            modelBuilder.Entity("backend_asa.Domain.Libro", b =>
                {
                    b.Property<int>("IdLibro")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdLibro"));

                    b.Property<string>("Autor")
                        .IsRequired()
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("Categoria")
                        .IsRequired()
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Isbn")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Titulo")
                        .IsRequired()
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("IdLibro");

                    b.HasIndex("Estado");

                    b.HasIndex("Isbn")
                        .IsUnique();

                    b.ToTable("Libros", t =>
                        {
                            t.HasCheckConstraint("CK_Libro_Estado", "[Estado] IN ('Activo', 'Inactivo')");
                        });
                });

            modelBuilder.Entity("backend_asa.Domain.Reserva", b =>
                {
                    b.Property<int>("IdReserva")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdReserva"));

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("varchar(20)");

                    b.Property<DateTime?>("FechaCancelacion")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("FechaSolicitud")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<int>("IdEjemplar")
                        .HasColumnType("int");

                    b.Property<int>("IdLector")
                        .HasColumnType("int");

                    b.HasKey("IdReserva");

                    b.HasIndex("Estado");

                    b.HasIndex("IdLector");

                    b.HasIndex(new[] { "IdEjemplar" }, "IX_Reservas_IdEjemplar");

                    b.HasIndex(new[] { "IdEjemplar" }, "IX_Reservas_IdEjemplar_Activa")
                        .IsUnique()
                        .HasFilter("[Estado] = 'Activa'");

                    b.ToTable("Reservas", t =>
                        {
                            t.HasCheckConstraint("CK_Reserva_Estado", "[Estado] IN ('Activa', 'Cancelada', 'Completada')");

                            t.HasCheckConstraint("CK_Reserva_FechaCancelacion", "([Estado] <> 'Cancelada') OR ([FechaCancelacion] IS NOT NULL)");
                        });
                });

            modelBuilder.Entity("backend_asa.Domain.Ejemplar", b =>
                {
                    b.HasOne("backend_asa.Domain.Libro", "Libro")
                        .WithMany("Ejemplares")
                        .HasForeignKey("IdLibro")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Libro");
                });

            modelBuilder.Entity("backend_asa.Domain.Reserva", b =>
                {
                    b.HasOne("backend_asa.Domain.Ejemplar", "Ejemplar")
                        .WithMany("Reservas")
                        .HasForeignKey("IdEjemplar")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("backend_asa.Domain.Lector", "Lector")
                        .WithMany("Reservas")
                        .HasForeignKey("IdLector")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Ejemplar");

                    b.Navigation("Lector");
                });

            modelBuilder.Entity("backend_asa.Domain.Ejemplar", b =>
                {
                    b.Navigation("Reservas");
                });

            modelBuilder.Entity("backend_asa.Domain.Lector", b =>
                {
                    b.Navigation("Reservas");
                });

            modelBuilder.Entity("backend_asa.Domain.Libro", b =>
                {
                    b.Navigation("Ejemplares");
                });
#pragma warning restore 612, 618
        }
    }
}
