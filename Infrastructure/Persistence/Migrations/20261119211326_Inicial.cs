using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_asa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lectores",
                columns: table => new
                {
                    IdLector = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identificador = table.Column<string>(type: "varchar(20)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    Correo = table.Column<string>(type: "varchar(150)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lectores", x => x.IdLector);
                    table.CheckConstraint("CK_Lector_Estado", "[Estado] IN ('Activo', 'Inactivo')");
                });

            migrationBuilder.CreateTable(
                name: "Libros",
                columns: table => new
                {
                    IdLibro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Isbn = table.Column<string>(type: "varchar(20)", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Autor = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libros", x => x.IdLibro);
                    table.CheckConstraint("CK_Libro_Estado", "[Estado] IN ('Activo', 'Inactivo')");
                });

            migrationBuilder.CreateTable(
                name: "Ejemplares",
                columns: table => new
                {
                    IdEjemplar = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdLibro = table.Column<int>(type: "int", nullable: false),
                    CodigoInventario = table.Column<string>(type: "varchar(30)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ejemplares", x => x.IdEjemplar);
                    table.CheckConstraint("CK_Ejemplar_Estado", "[Estado] IN ('Disponible', 'Reservado', 'Prestado', 'Inactivo')");
                    table.ForeignKey(
                        name: "FK_Ejemplares_Libros_IdLibro",
                        column: x => x.IdLibro,
                        principalTable: "Libros",
                        principalColumn: "IdLibro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    IdReserva = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdLector = table.Column<int>(type: "int", nullable: false),
                    IdEjemplar = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Estado = table.Column<string>(type: "varchar(20)", nullable: false),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.IdReserva);
                    table.CheckConstraint("CK_Reserva_Estado", "[Estado] IN ('Activa', 'Cancelada', 'Completada')");
                    table.CheckConstraint("CK_Reserva_FechaCancelacion", "([Estado] <> 'Cancelada') OR ([FechaCancelacion] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Reservas_Ejemplares_IdEjemplar",
                        column: x => x.IdEjemplar,
                        principalTable: "Ejemplares",
                        principalColumn: "IdEjemplar",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Lectores_IdLector",
                        column: x => x.IdLector,
                        principalTable: "Lectores",
                        principalColumn: "IdLector",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ejemplares_CodigoInventario",
                table: "Ejemplares",
                column: "CodigoInventario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ejemplares_Estado",
                table: "Ejemplares",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Ejemplares_IdLibro",
                table: "Ejemplares",
                column: "IdLibro");

            migrationBuilder.CreateIndex(
                name: "IX_Ejemplares_IdLibro_Estado",
                table: "Ejemplares",
                columns: new[] { "IdLibro", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Lectores_Correo",
                table: "Lectores",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lectores_Identificador",
                table: "Lectores",
                column: "Identificador",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Libros_Estado",
                table: "Libros",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Libros_Isbn",
                table: "Libros",
                column: "Isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Estado",
                table: "Reservas",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_IdEjemplar",
                table: "Reservas",
                column: "IdEjemplar");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_IdEjemplar_Activa",
                table: "Reservas",
                column: "IdEjemplar",
                unique: true,
                filter: "[Estado] = 'Activa'");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_IdLector",
                table: "Reservas",
                column: "IdLector");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Ejemplares");

            migrationBuilder.DropTable(
                name: "Lectores");

            migrationBuilder.DropTable(
                name: "Libros");
        }
    }
}
