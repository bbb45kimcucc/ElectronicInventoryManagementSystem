using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockCardsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public StockCardsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockCard>>> GetStockCards()
        {
            return await _context.StockCards.Include(s => s.Product).ToListAsync();
        }

        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<StockCard>>> GetByProduct(int productId)
        {
            return await _context.StockCards
                .Where(s => s.ProductId == productId)
                .OrderByDescending(s => s.TransactionDate)
                .ToListAsync();
        }
    }
}