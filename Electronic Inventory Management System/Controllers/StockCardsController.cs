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

        // 1. Xem tất cả (Có phân trang cho khỏi lag)
        [HttpGet]
        public async Task<ActionResult> GetStockCards([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var totalItems = await _context.StockCards.CountAsync();
            var cards = await _context.StockCards
                .Include(s => s.Product)
                .OrderByDescending(s => s.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Data = cards
            });
        }

        // 2. Tra cứu lịch sử thẻ kho của 1 Sản phẩm (Có lọc theo Ngày tháng)
        // Gọi mẫu: api/StockCards/product/1?startDate=2024-01-01&endDate=2024-01-31
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<StockCard>>> GetByProduct(
            int productId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.StockCards.Where(s => s.ProductId == productId).AsQueryable();

            // Lọc từ ngày
            if (startDate.HasValue)
            {
                query = query.Where(s => s.TransactionDate >= startDate.Value);
            }

            // Lọc đến ngày
            if (endDate.HasValue)
            {
                query = query.Where(s => s.TransactionDate <= endDate.Value);
            }

            var result = await query
                .OrderByDescending(s => s.TransactionDate)
                .ToListAsync();

            return Ok(result);
        }

        // 3. Thêm mới thẻ kho (Dùng khi kiểm kê kho hoặc nhập đầu kỳ)
        [HttpPost]
        public async Task<ActionResult<StockCard>> PostStockCard(StockCard stockCard)
        {
            stockCard.TransactionDate = DateTime.Now;

            _context.StockCards.Add(stockCard);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã ghi nhận lịch sử vào Thẻ kho thành công!", data = stockCard });
        }
    }
}