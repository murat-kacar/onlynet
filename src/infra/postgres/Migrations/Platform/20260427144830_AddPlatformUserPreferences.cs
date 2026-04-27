using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabFlow.Migrations.Migrations.Platform
{
    /// <inheritdoc />
    public partial class AddPlatformUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "IntendedOwnerEmail",
                table: "tenants",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AlterColumn<string>(
                name: "DatabasePassword",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "DataClass: Restricted",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "platform_audit_log",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResourceType",
                table: "platform_audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "DataClass: Internal",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ResourceId",
                table: "platform_audit_log",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "DataClass: Internal",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Ip",
                table: "platform_audit_log",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Changes",
                table: "platform_audit_log",
                type: "jsonb",
                nullable: true,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActorEmail",
                table: "platform_audit_log",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "platform_audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "DataClass: Internal",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "platform_user_preferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Density = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_user_preferences", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_user_preferences");

            migrationBuilder.AlterColumn<string>(
                name: "IntendedOwnerEmail",
                table: "tenants",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "DatabasePassword",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "DataClass: Restricted");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "platform_audit_log",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "ResourceType",
                table: "platform_audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "DataClass: Internal");

            migrationBuilder.AlterColumn<string>(
                name: "ResourceId",
                table: "platform_audit_log",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "DataClass: Internal");

            migrationBuilder.AlterColumn<string>(
                name: "Ip",
                table: "platform_audit_log",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Changes",
                table: "platform_audit_log",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "ActorEmail",
                table: "platform_audit_log",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "platform_audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "DataClass: Internal");
        }
    }
}
