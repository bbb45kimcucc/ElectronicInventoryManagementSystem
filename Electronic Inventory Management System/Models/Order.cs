using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicInventoryManagementSystem.Models;

public class Order
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string TrackingId { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string Type { get; set; } = "Home Delivery"; 

    [StringLength(50)]
    public string Status { get; set; } = "Pending"; 

    public decimal TotalAmount { get; set; } = 0; 

    // === CÁC KHÓA NGOẠI ===
    public int CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int? UserId { get; set; } 
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    // Một đơn hàng có nhiều chi tiết món hàng
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}