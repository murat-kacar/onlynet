using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabFlow.Migrations.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddCustomerAccessTicketDeviceCookie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceCookieValue",
                table: "customer_access_tickets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceCookieValue",
                table: "customer_access_tickets");
        }
    }
}
