using Microsoft.AspNetCore.Http; // Nhớ có dòng này để dùng được HttpContext

namespace ElectronicInventoryManagementSystem.Helpers
{
    public static class AuthHelper
    {
        public static bool IsAdmin(HttpContext context)
        {
            var role = context.Session.GetString("UserRole");

            return role == "Admin";
        }
    }
}