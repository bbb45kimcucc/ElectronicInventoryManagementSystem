using ElectronicInventoryManagementSystem.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class InventoryLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty; 
    public string Entity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}