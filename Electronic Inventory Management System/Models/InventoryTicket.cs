
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

   
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

   
    public int? SupplierId { get; set; }
    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

 
    public virtual ICollection<TicketDetail> TicketDetails { get; set; } = new List<TicketDetail>();
}