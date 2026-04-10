namespace ElectronicInventoryManagementSystem.Helpers
{
    public static class AuthHelper
    {
        public static bool IsAdmin(HttpRequest request)
        {
            var role = request.Headers["User-Role"].ToString();
            return role == "Admin";
        }
    }
}