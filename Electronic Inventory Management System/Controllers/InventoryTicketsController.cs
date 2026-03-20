using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTicketsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public InventoryTicketsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryTicket>>> GetTickets()
        {
            return await _context.InventoryTickets
                .Include(t => t.User)
                .Include(t => t.Customer)
                .Include(t => t.TicketDetails)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<InventoryTicket>> PostTicket(InventoryTicket ticket)
        {
            _context.InventoryTickets.Add(ticket);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTickets), new { id = ticket.Id }, ticket);
        }
    }
}