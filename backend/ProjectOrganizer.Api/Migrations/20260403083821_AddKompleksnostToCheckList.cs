using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddKompleksnostToCheckList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Kompleksnost",
                table: "ProjectImplementationItemCheckLists",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Kompleksnost",
                table: "CheckListItems",
                type: "decimal(5,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kompleksnost",
                table: "ProjectImplementationItemCheckLists");

            migrationBuilder.AlterColumn<decimal>(
                name: "Kompleksnost",
                table: "CheckListItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);
        }
    }
}
