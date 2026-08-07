using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Subify.Infrastructure.Persistence;

#nullable disable

namespace Subify.Infrastructure.Migrations
{
    /// <summary>Removes short-lived family budget opt-in flags (feature cancelled).</summary>
    [DbContext(typeof(SubifyDbContext))]
    [Migration("20260802120000_DropFamilyBudgetFlags")]
    public partial class DropFamilyBudgetFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FamilyBudgetEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "ShareInFamilyBudget",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FamilyBudgetEnabled",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShareInFamilyBudget",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
