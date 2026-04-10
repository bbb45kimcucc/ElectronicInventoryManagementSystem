using System.ComponentModel.DataAnnotations;

namespace ElectronicInventoryManagementSystem.Models;

public class Manufacturer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; 

    [StringLength(100)]
    public string? Country { get; set; } 

  
}