using System.ComponentModel.DataAnnotations;

namespace ElectronicInventoryManagementSystem.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty; 

    public string? Email { get; set; }
    public string? Phone { get; set; }
}