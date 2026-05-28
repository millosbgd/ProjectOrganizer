using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectOrganizer.Api.Data;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260528070000_AddDevOpsSyncMetadata")]
    public partial class AddDevOpsSyncMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DevOpsAssignedTo",
                table: "DevOpsTasksCandidates",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DevOpsChangedDate",
                table: "DevOpsTasksCandidates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevOpsState",
                table: "DevOpsTasksCandidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDevOpsSyncAt",
                table: "DevOpsTasksCandidates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastDevOpsSyncError",
                table: "DevOpsTasksCandidates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastDevOpsSyncStatus",
                table: "DevOpsTasksCandidates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DevOpsAssignedTo",
                table: "DevOpsTasksCandidates");

            migrationBuilder.DropColumn(
                name: "DevOpsChangedDate",
                table: "DevOpsTasksCandidates");

            migrationBuilder.DropColumn(
                name: "DevOpsState",
                table: "DevOpsTasksCandidates");

            migrationBuilder.DropColumn(
                name: "LastDevOpsSyncAt",
                table: "DevOpsTasksCandidates");

            migrationBuilder.DropColumn(
                name: "LastDevOpsSyncError",
                table: "DevOpsTasksCandidates");

            migrationBuilder.DropColumn(
                name: "LastDevOpsSyncStatus",
                table: "DevOpsTasksCandidates");
        }
    }
}
