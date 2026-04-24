
    namespace ElectronicInventoryManagementSystem.Helpers
    {
        public static class RoleConstants
        {
            public const string Admin = "Admin";
            public const string Staff = "Staff";

            public const string CanDelete = "CanDeleteData"; // Chỉ Admin
            public const string CanManageUser = "CanManageUser"; // Chỉ Admin
            public const string CanCreateTicket = "CanCreateTicket"; // Cả 2 đều làm được
        }
    }

