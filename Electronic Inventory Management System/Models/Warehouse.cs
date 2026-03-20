using System.ComponentModel.DataAnnotations;

namespace ElectronicInventoryManagementSystem.Models
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; 

        [StringLength(200)]
        public string? Location { get; set; }

        public string? Description { get; set; }
    }
}