using System.ComponentModel.DataAnnotations;

namespace ElectronicInventoryManagementSystem.Models;

public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty; 

    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
}