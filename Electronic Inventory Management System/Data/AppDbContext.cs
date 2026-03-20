
using ElectronicInventoryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicInventoryManagementSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<InventoryTicket> InventoryTickets { get; set; }
        public DbSet<TicketDetail> TicketDetails { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<StockCard> StockCards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TicketDetail>()
                .HasOne(d => d.Ticket)
                .WithMany(t => t.TicketDetails)
                .HasForeignKey(d => d.TicketId);

            modelBuilder.Entity<TicketDetail>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId);
        }
    }
}