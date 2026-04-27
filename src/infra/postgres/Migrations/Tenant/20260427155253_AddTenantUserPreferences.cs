using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabFlow.Migrations.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTenantUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "tenant_audit_log",
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
                table: "tenant_audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "DataClass: Internal",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ResourceId",
                table: "tenant_audit_log",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "DataClass: Internal",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Ip",
                table: "tenant_audit_log",
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
                table: "tenant_audit_log",
                type: "jsonb",
                nullable: true,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActorEmail",
                table: "tenant_audit_log",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "tenant_audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "DataClass: Internal",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "qr_tokens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "DataClass: Restricted",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "orders",
                type: "text",
                nullable: true,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "order_items",
                type: "text",
                nullable: true,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceKeyHash",
                table: "device_keys",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "DataClass: Restricted",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "customer_session_cart_items",
                type: "text",
                nullable: true,
                comment: "DataClass: Sensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceCookieValue",
                table: "customer_access_tickets",
                type: "text",
                nullable: false,
                comment: "DataClass: Restricted",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "bills",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "DataClass: Restricted",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "tenant_user_preferences",
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
                    table.PrimaryKey("PK_tenant_user_preferences", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_user_preferences");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "tenant_audit_log",
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
                table: "tenant_audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "DataClass: Internal");

            migrationBuilder.AlterColumn<string>(
                name: "ResourceId",
                table: "tenant_audit_log",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "DataClass: Internal");

            migrationBuilder.AlterColumn<string>(
                name: "Ip",
                table: "tenant_audit_log",
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
                table: "tenant_audit_log",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "ActorEmail",
                table: "tenant_audit_log",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "tenant_audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "DataClass: Internal");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "qr_tokens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "DataClass: Restricted");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "order_items",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceKeyHash",
                table: "device_keys",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "DataClass: Restricted");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "customer_session_cart_items",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "DataClass: Sensitive");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceCookieValue",
                table: "customer_access_tickets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "DataClass: Restricted");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "bills",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "DataClass: Restricted");
        }
    }
}
