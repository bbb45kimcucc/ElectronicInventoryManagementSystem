using ElectronicInventoryManagementSystem.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}