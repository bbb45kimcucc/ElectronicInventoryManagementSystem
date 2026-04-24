using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicInventoryManagementSystem.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    [NotMapped] 
    public string? Image { get; set; }

    public int? Quantity { get; set; }
    public int? CurrentStock { get; set; } = 0;

    public double AveragePrice { get; set; } = 0;

    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    public int? WarehouseId { get; set; }

    [ForeignKey("WarehouseId")]
    public virtual Warehouse? Warehouse { get; set; }

    public int? UnitId { get; set; }
    public int? BrandId { get; set; }

    [ForeignKey("UnitId")]
    public virtual Unit? Unit { get; set; }

    [ForeignKey("BrandId")]
    public virtual Brand? Brand { get; set; }

    public virtual ICollection<ProductImage>? Images { get; set; }
}