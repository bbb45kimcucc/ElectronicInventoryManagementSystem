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

        // 1. Lấy danh sách phiếu 
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
        // 3. THÊM PHIẾU VÀ TỰ ĐỘNG CẬP NHẬT TỒN KHO + GHI THẺ KHO
        [HttpPost]
        public async Task<ActionResult<InventoryTicket>> PostTicket(InventoryTicket ticket)
        {
            if (ticket.TicketDetails == null || !ticket.TicketDetails.Any())
            {
                return BadRequest(new { message = "Phiếu phải có ít nhất 1 sản phẩm bên trong." });
            }

            // 1. Lưu Phiếu Kho trước để nó lấy ID (Cần ID để làm Khóa chính)
            ticket.CreatedAt = DateTime.Now;
            _context.InventoryTickets.Add(ticket);

            // Phải SaveChanges 1 lần ở đây để ticket có cái ID thật trong Database
            await _context.SaveChangesAsync();

            // 2. Lặp qua từng chi tiết hàng hóa để xử lý tồn kho và Thẻ kho
            foreach (var detail in ticket.TicketDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product == null)
                    return BadRequest(new { message = $"Sản phẩm có ID {detail.ProductId} không tồn tại." });

                // Lấy số lượng Tồn đầu kỳ (Trước khi thay đổi)
                int beforeQty = product.Quantity;
                int changeQty = detail.Quantity;

                // Xử lý logic Nhập / Xuất 
                if (ticket.Type.ToLower() == "nhập")
                {
                    product.Quantity += changeQty; // Nhập thì cộng kho
                }
                else if (ticket.Type.ToLower() == "xuất")
                {
                    if (product.Quantity < changeQty)
                    {
                        // Chặn xuất âm kho, đồng thời phải xóa cái Phiếu vừa tạo hụt ở trên
                        _context.InventoryTickets.Remove(ticket);
                        await _context.SaveChangesAsync();
                        return BadRequest(new { message = $"Sản phẩm '{product.Name}' không đủ số lượng để xuất. Tồn: {product.Quantity}" });
                    }
                    product.Quantity -= changeQty; // Xuất thì trừ kho
                    changeQty = -changeQty; // Đổi dấu thành số âm để ghi vào Thẻ kho cho chuẩn
                }

                // ==========================================
                // MA THUẬT TỰ ĐỘNG: Tạo ngay 1 Thẻ Kho (StockCard)
                // ==========================================
                var stockCard = new StockCard
                {
                    ProductId = product.Id,
                    TransactionDate = DateTime.Now,
                    ReferenceCode = ticket.TicketCode, // Móc mã phiếu vào Thẻ kho
                    BeforeQty = beforeQty,             // Tồn đầu
                    ChangeQty = changeQty,             // Thay đổi (+ hoặc -)
                    AfterQty = product.Quantity,       // Tồn cuối
                    Note = $"Lập phiếu {ticket.Type.ToLower()} tự động"
                };
                _context.StockCards.Add(stockCard);
            }

            // Lưu một lần cuối cùng (Gồm Tồn kho mới + Danh sách Thẻ kho)
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }

        // 4. XÓA PHIẾU VÀ HOÀN LẠI TỒN KHO + GHI THẺ KHO HOÀN TÁC
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
                    int beforeQty = product.Quantity;
                    int changeQty = detail.Quantity;

                    if (ticket.Type.ToLower() == "nhập")
                    {
                        product.Quantity -= changeQty; // Xóa phiếu Nhập -> Bị trừ kho
                        changeQty = -changeQty; // Ghi số âm
                    }
                    else if (ticket.Type.ToLower() == "xuất")
                    {
                        product.Quantity += changeQty; // Xóa phiếu Xuất -> Được cộng kho lại
                    }

                    // Ghi nhận lịch sử hoàn tác vào Thẻ kho
                    var stockCard = new StockCard
                    {
                        ProductId = product.Id,
                        TransactionDate = DateTime.Now,
                        ReferenceCode = "HỦY-" + ticket.TicketCode,
                        BeforeQty = beforeQty,
                        ChangeQty = changeQty,
                        AfterQty = product.Quantity,
                        Note = $"Hủy phiếu {ticket.Type.ToLower()} - Hoàn lại kho"
                    };
                    _context.StockCards.Add(stockCard);
                }
            }

            _context.InventoryTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa phiếu, hoàn tồn kho và ghi nhật ký thành công." });
        }
    }
}