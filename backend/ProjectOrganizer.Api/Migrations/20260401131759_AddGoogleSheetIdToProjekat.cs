using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleSheetIdToProjekat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AIPracen",
                table: "Projekti",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GoogleSheetId",
                table: "Projekti",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AktivnostId = table.Column<int>(type: "int", nullable: false),
                    ProjekatId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RemindAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiReminders_Aktivnosti_AktivnostId",
                        column: x => x.AktivnostId,
                        principalTable: "Aktivnosti",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiReminders_Projekti_ProjekatId",
                        column: x => x.ProjekatId,
                        principalTable: "Projekti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AiReminders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DailyTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    KorisnikKreirao = table.Column<int>(type: "int", nullable: false),
                    Solved = table.Column<bool>(type: "bit", nullable: false),
                    Pinned = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyTasks_Users_KorisnikKreirao",
                        column: x => x.KorisnikKreirao,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProjekatId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Dismissed = table.Column<bool>(type: "bit", nullable: false),
                    AktivnostId = table.Column<int>(type: "int", nullable: true),
                    ReferenceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Aktivnosti_AktivnostId",
                        column: x => x.AktivnostId,
                        principalTable: "Aktivnosti",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Projekti_ProjekatId",
                        column: x => x.ProjekatId,
                        principalTable: "Projekti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiReminders_AktivnostId",
                table: "AiReminders",
                column: "AktivnostId");

            migrationBuilder.CreateIndex(
                name: "IX_AiReminders_ProjekatId",
                table: "AiReminders",
                column: "ProjekatId");

            migrationBuilder.CreateIndex(
                name: "IX_AiReminders_RemindAt_Sent",
                table: "AiReminders",
                columns: new[] { "RemindAt", "Sent" });

            migrationBuilder.CreateIndex(
                name: "IX_AiReminders_UserId",
                table: "AiReminders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyTasks_KorisnikKreirao",
                table: "DailyTasks",
                column: "KorisnikKreirao");

            migrationBuilder.CreateIndex(
                name: "IX_DailyTasks_KorisnikKreirao_Datum",
                table: "DailyTasks",
                columns: new[] { "KorisnikKreirao", "Datum" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AktivnostId",
                table: "Notifications",
                column: "AktivnostId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ProjekatId",
                table: "Notifications",
                column: "ProjekatId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReferenceKey",
                table: "Notifications",
                column: "ReferenceKey",
                filter: "[ReferenceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiReminders");

            migrationBuilder.DropTable(
                name: "DailyTasks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "AIPracen",
                table: "Projekti");

            migrationBuilder.DropColumn(
                name: "GoogleSheetId",
                table: "Projekti");
        }
    }
}
