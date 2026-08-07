using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstanceDefaultTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultApplicationThemeColor",
                table: "SystemSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Royal Purple");

            migrationBuilder.AddColumn<bool>(
                name: "DefaultDarkTheme",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultApplicationThemeColor",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultDarkTheme",
                table: "SystemSettings");
        }
    }
}
