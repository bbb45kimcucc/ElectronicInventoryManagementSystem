using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Electronic_Inventory_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FullName", "Password", "Role", "Username" },
                values: new object[] { 1, "admin@gmail.com", "Admin Kim Cúc", "112233", "Admin", "admin_cuc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
