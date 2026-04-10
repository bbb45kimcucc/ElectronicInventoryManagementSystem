using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicInventoryManagementSystem.Models;

public class OrderDetail
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    // Mua sản phẩm (linh kiện) nào?
    public int ProductId { get; set; }
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    public int Quantity { get; set; } // Số lượng mua
    public decimal UnitPrice { get; set; } // Giá lúc mua (đề phòng mốt linh kiện tăng giá)
}