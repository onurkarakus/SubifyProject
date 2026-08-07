using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Subify.Infrastructure.Persistence;

#nullable disable

namespace Subify.Infrastructure.Migrations
{
    /// <summary>Removes subscription LastUsedAt / “used today” tracking (feature cancelled).</summary>
    [DbContext(typeof(SubifyDbContext))]
    [Migration("20260802130000_DropSubscriptionLastUsedAt")]
    public partial class DropSubscriptionLastUsedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "Subscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "LastUsedAt",
                table: "Subscriptions",
                type: "date",
                nullable: true);
        }
    }
}
