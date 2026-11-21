using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_asa.Infrastructure.Persistence.Migrations
{
    public partial class AgregarIndiceLectoresEstado : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Lectores_Estado",
                table: "Lectores",
                column: "Estado");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lectores_Estado",
                table: "Lectores");
        }
    }
}
