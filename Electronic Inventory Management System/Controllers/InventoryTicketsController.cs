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

        // 1. Lấy danh sách phiếu (Kèm chi tiết, người tạo, khách/nhà cung cấp)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryTicket>>> GetTickets()
        {
            return await _context.InventoryTickets
                .Include(t => t.User)
                .Include(t => t.Customer)
                .Include(t => t.Supplier)
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Product) // Lấy luôn tên sản phẩm trong chi tiết
                .OrderByDescending(t => t.CreatedAt) // Phiếu mới nhất lên đầu
                .ToListAsync();
        }

        // 2. Lấy chi tiết 1 phiếu
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryTicket>> GetTicket(int id)
        {
            var ticket = await _context.InventoryTickets
                .Include(t => t.User)
                .Include(t => t.Customer)
                .Include(t => t.Supplier)
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound(new { message = "Không tìm thấy phiếu này." });
            return ticket;
        }

        // 3. THÊM PHIẾU VÀ TỰ ĐỘNG CẬP NHẬT TỒN KHO (NGHIỆP VỤ LÕI)
        [HttpPost]
        public async Task<ActionResult<InventoryTicket>> PostTicket(InventoryTicket ticket)
        {
            if (ticket.TicketDetails == null || !ticket.TicketDetails.Any())
            {
                return BadRequest(new { message = "Phiếu phải có ít nhất 1 sản phẩm bên trong." });
            }

            // Lặp qua từng chi tiết hàng hóa trong phiếu để xử lý kho
            foreach (var detail in ticket.TicketDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product == null)
                    return BadRequest(new { message = $"Sản phẩm có ID {detail.ProductId} không tồn tại." });

                // Xử lý logic Nhập / Xuất (Giả sử Type là "Nhập" hoặc "Xuất")
                if (ticket.Type.ToLower() == "nhập")
                {
                    product.Quantity += detail.Quantity; // Nhập thì cộng kho
                }
                else if (ticket.Type.ToLower() == "xuất")
                {
                    if (product.Quantity < detail.Quantity)
                    {
                        // Chặn xuất âm kho
                        return BadRequest(new { message = $"Sản phẩm '{product.Name}' không đủ số lượng để xuất. Tồn kho hiện tại: {product.Quantity}" });
                    }
                    product.Quantity -= detail.Quantity; // Xuất thì trừ kho
                }
            }

            // EF Core sẽ tự động dùng Transaction. Lưu Phiếu, Lưu Chi Tiết, Lưu Tồn Kho cùng 1 lúc!
            _context.InventoryTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }

        // 4. XÓA PHIẾU VÀ HOÀN LẠI TỒN KHO
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.InventoryTickets
                .Include(t => t.TicketDetails)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound(new { message = "Không tìm thấy phiếu." });

            // Trả lại kho trước khi xóa phiếu
            foreach (var detail in ticket.TicketDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    if (ticket.Type.ToLower() == "nhập")
                        product.Quantity -= detail.Quantity; // Xóa phiếu nhập -> Trừ kho lại
                    else if (ticket.Type.ToLower() == "xuất")
                        product.Quantity += detail.Quantity; // Xóa phiếu xuất -> Cộng kho lại
                }
            }

            _context.InventoryTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa phiếu và cập nhật lại số lượng tồn kho." });
        }
    }
}