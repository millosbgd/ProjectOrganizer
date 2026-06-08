using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectOrganizer.Api.Data;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260608100000_AddClientVisitsAndFuelPurchases")]
    public partial class AddClientVisitsAndFuelPurchases : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KlijentId = table.Column<int>(type: "int", nullable: false),
                    AktivnostId = table.Column<int>(type: "int", nullable: false),
                    Grad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kilometraza = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    GorivoLitara = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientVisits_Aktivnosti_AktivnostId",
                        column: x => x.AktivnostId,
                        principalTable: "Aktivnosti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientVisits_Klijenti_KlijentId",
                        column: x => x.KlijentId,
                        principalTable: "Klijenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientVisits_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FuelPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    JedinicnaCena = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UkupnaCena = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelPurchases_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientVisits_AktivnostId",
                table: "ClientVisits",
                column: "AktivnostId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientVisits_CreatedBy",
                table: "ClientVisits",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ClientVisits_KlijentId",
                table: "ClientVisits",
                column: "KlijentId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelPurchases_CreatedBy",
                table: "FuelPurchases",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FuelPurchases_Datum",
                table: "FuelPurchases",
                column: "Datum");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ClientVisits");
            migrationBuilder.DropTable(name: "FuelPurchases");
        }
    }
}
