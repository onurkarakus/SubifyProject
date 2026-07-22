using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLocateToLocale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Locate",
                table: "AspNetUsers",
                newName: "Locale");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Locale",
                table: "AspNetUsers",
                newName: "Locate");
        }
    }
}
