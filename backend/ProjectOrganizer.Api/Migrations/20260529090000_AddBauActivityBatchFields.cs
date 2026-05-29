using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectOrganizer.Api.Data;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260529090000_AddBauActivityBatchFields")]
    public partial class AddBauActivityBatchFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BauTipAktivnosti",
                table: "Aktivnosti",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BauTrajanjeMinuta",
                table: "Aktivnosti",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KlijentId",
                table: "Aktivnosti",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Aktivnosti_KlijentId",
                table: "Aktivnosti",
                column: "KlijentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Aktivnosti_Klijenti_KlijentId",
                table: "Aktivnosti",
                column: "KlijentId",
                principalTable: "Klijenti",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(@"
DECLARE @EntityTypeId INT;

SELECT @EntityTypeId = Id
FROM dbo.CodebookEntities
WHERE Name = 'BauActivityType';

IF @EntityTypeId IS NULL
BEGIN
    INSERT INTO dbo.CodebookEntities (Name, Description, IsActive, CreatedAt)
    VALUES ('BauActivityType', 'Tipovi BAU aktivnosti za zbirni unos', 1, SYSUTCDATETIME());

    SET @EntityTypeId = SCOPE_IDENTITY();
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'SUPPORT')
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'SUPPORT', 'Podrška', 10, 1, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'CONSULTING')
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'CONSULTING', 'Konsultacije', 20, 1, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'ANALYSIS')
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'ANALYSIS', 'Analiza', 30, 1, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'ADMIN')
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'ADMIN', 'Administracija', 40, 1, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'COMMUNICATION')
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'COMMUNICATION', 'Komunikacija', 50, 1, SYSUTCDATETIME());
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aktivnosti_Klijenti_KlijentId",
                table: "Aktivnosti");

            migrationBuilder.DropIndex(
                name: "IX_Aktivnosti_KlijentId",
                table: "Aktivnosti");

            migrationBuilder.DropColumn(
                name: "BauTipAktivnosti",
                table: "Aktivnosti");

            migrationBuilder.DropColumn(
                name: "BauTrajanjeMinuta",
                table: "Aktivnosti");

            migrationBuilder.DropColumn(
                name: "KlijentId",
                table: "Aktivnosti");
        }
    }
}
