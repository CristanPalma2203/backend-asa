using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_asa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuitarIndiceEjemplaresIdLibro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ejemplares_IdLibro",
                table: "Ejemplares");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ejemplares_IdLibro",
                table: "Ejemplares",
                column: "IdLibro");
        }
    }
}
