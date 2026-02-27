using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPdvFieldsToKlijenti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdvRegistrationDate",
                table: "Klijenti",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdvStatus",
                table: "Klijenti",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdvRegistrationDate",
                table: "Klijenti");

            migrationBuilder.DropColumn(
                name: "PdvStatus",
                table: "Klijenti");
        }
    }
}
