using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicInventoryManagementSystem.Models;

public class InventoryTicket
{
    [Key]
    public int Id { get; set; }

    public string TicketCode { get; set; } = string.Empty;

    public string Type { get; set; } = "Import";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // THÊM: Tổng số lượng hàng trong phiếu (Ví dụ: 10 IC + 20 Tụ = 30)
    public int TotalQuantity { get; set; } = 0;

    // THÊM: Tổng tiền của cả phiếu (Cực kỳ cần thiết cho đồ án)
    public decimal TotalAmount { get; set; } = 0;

    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    public int? SupplierId { get; set; }
    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<TicketDetail> TicketDetails { get; set; } = new List<TicketDetail>();
}