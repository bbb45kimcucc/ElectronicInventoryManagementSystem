using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicInventoryManagementSystem.Models
{
    public class TicketDetail
    {
        [Key]
        public int Id { get; set; }

       
        public int TicketId { get; set; }
        [ForeignKey("TicketId")]
        public virtual InventoryTicket? Ticket { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
    }
}