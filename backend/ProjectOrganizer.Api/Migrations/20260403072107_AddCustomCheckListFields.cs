using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomCheckListFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetaljanOpis",
                table: "ProjectImplementationItemCheckLists",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Opis",
                table: "ProjectImplementationItemCheckLists",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlaniraniRok",
                table: "ProjectImplementationItemCheckLists",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Mailovi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    From = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Cc = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(998)", maxLength: 998, nullable: false),
                    BodyText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatumUcitavanja = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mailovi", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mailovi_UserId",
                table: "Mailovi",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Mailovi_UserId_MessageId",
                table: "Mailovi",
                columns: new[] { "UserId", "MessageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mailovi");

            migrationBuilder.DropColumn(
                name: "DetaljanOpis",
                table: "ProjectImplementationItemCheckLists");

            migrationBuilder.DropColumn(
                name: "Opis",
                table: "ProjectImplementationItemCheckLists");

            migrationBuilder.DropColumn(
                name: "PlaniraniRok",
                table: "ProjectImplementationItemCheckLists");
        }
    }
}
