using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Electronic_Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class InitDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "InventoryTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "InventoryTickets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuantity",
                table: "InventoryTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTickets_CustomerId",
                table: "InventoryTickets",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTickets_Customers_CustomerId",
                table: "InventoryTickets",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTickets_Customers_CustomerId",
                table: "InventoryTickets");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTickets_CustomerId",
                table: "InventoryTickets");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "InventoryTickets");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "InventoryTickets");

            migrationBuilder.DropColumn(
                name: "TotalQuantity",
                table: "InventoryTickets");
        }
    }
}
