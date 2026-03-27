using System.ComponentModel.DataAnnotations;

namespace ElectronicInventoryManagementSystem.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty; 

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; }

    public string Role { get; set; } = "Staff"; // Quyền: Admin hoặc Staff (Nhân viên)
}