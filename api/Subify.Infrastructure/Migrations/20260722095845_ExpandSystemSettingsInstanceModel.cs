using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Subify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSystemSettingsInstanceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AIApiKey",
                table: "SystemSettings",
                newName: "AiApiKey");

            migrationBuilder.AlterColumn<string>(
                name: "SmtpUser",
                table: "SystemSettings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SmtpHost",
                table: "SystemSettings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SmtpFromName",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SmtpFromEmail",
                table: "SystemSettings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModel",
                table: "SystemSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiProvider",
                table: "SystemSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPublicRegistration",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "SystemSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<string>(
                name: "DefaultLocale",
                table: "SystemSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "tr");

            migrationBuilder.AddColumn<string>(
                name: "InstanceName",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSetupComplete",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SetupCompletedAt",
                table: "SystemSettings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SmtpEnabled",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "SystemSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiModel",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AiProvider",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AllowPublicRegistration",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultLocale",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "InstanceName",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IsSetupComplete",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SetupCompletedAt",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SmtpEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "SystemSettings");

            migrationBuilder.RenameColumn(
                name: "AiApiKey",
                table: "SystemSettings",
                newName: "AIApiKey");

            migrationBuilder.AlterColumn<string>(
                name: "SmtpUser",
                table: "SystemSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SmtpHost",
                table: "SystemSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SmtpFromName",
                table: "SystemSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SmtpFromEmail",
                table: "SystemSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldNullable: true);
        }
    }
}
