using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSendLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSendLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    DedupeKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSendLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLogs_DedupeKey",
                table: "EmailSendLogs",
                column: "DedupeKey",
                unique: true,
                filter: "\"DedupeKey\" IS NOT NULL AND \"Success\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLogs_TemplateName_SentAt",
                table: "EmailSendLogs",
                columns: new[] { "TemplateName", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendLogs_UserId",
                table: "EmailSendLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSendLogs");
        }
    }
}
