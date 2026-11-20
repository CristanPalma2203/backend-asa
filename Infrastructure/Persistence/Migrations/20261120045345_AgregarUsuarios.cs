using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_asa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Correo = table.Column<string>(type: "varchar(150)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Rol = table.Column<string>(type: "varchar(20)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", nullable: false),
                    IdLector = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                    table.CheckConstraint("CK_Usuario_Estado", "[Estado] IN ('Activo', 'Inactivo')");
                    table.CheckConstraint("CK_Usuario_Rol", "[Rol] IN ('Admin', 'Lector')");
                    table.ForeignKey(
                        name: "FK_Usuarios_Lectores_IdLector",
                        column: x => x.IdLector,
                        principalTable: "Lectores",
                        principalColumn: "IdLector",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdLector",
                table: "Usuarios",
                column: "IdLector",
                unique: true,
                filter: "[IdLector] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
