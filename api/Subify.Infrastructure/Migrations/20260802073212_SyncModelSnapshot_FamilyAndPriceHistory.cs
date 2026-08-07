using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot_FamilyAndPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "SubscriptionPriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OldCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPriceHistories_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPriceHistories_SubscriptionId_ChangedAt",
                table: "SubscriptionPriceHistories",
                columns: new[] { "SubscriptionId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPriceHistories_UserId",
                table: "SubscriptionPriceHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPriceHistories");

            migrationBuilder.DropColumn(
                name: "FamilyBudgetEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "ShareInFamilyBudget",
                table: "AspNetUsers");
        }
    }
}
