using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddKlijentPotvrdioToCheckListItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "KlijentPotvrdio",
                table: "ProjectImplementationItemCheckLists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "KlijentPotvrdioDatum",
                table: "ProjectImplementationItemCheckLists",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KlijentPotvrdio",
                table: "ProjectImplementationItemCheckLists");

            migrationBuilder.DropColumn(
                name: "KlijentPotvrdioDatum",
                table: "ProjectImplementationItemCheckLists");
        }
    }
}
