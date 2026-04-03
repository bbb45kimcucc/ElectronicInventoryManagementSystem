namespace ElectronicInventoryManagementSystem.Helpers
{
    public static class AuthHelper
    {
        public static string CurrentRole = "Staff";

        public static bool IsAdmin()
        {
            return CurrentRole == "Admin";
        }
    }
}