using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicInventoryManagementSystem.Models
{
    public class StockCard
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string ReferenceCode { get; set; } = string.Empty; 

        public int BeforeQty { get; set; }  
        public int ChangeQty { get; set; }  
        public int AfterQty { get; set; } 

        public string? Note { get; set; }
    }
}