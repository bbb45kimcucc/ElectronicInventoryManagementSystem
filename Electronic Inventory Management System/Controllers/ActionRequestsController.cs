using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;
using ElectronicInventoryManagementSystem.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 👈 Đảm bảo ai gọi API này cũng phải đăng nhập
    public class ActionRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ActionRequestsController(AppDbContext context) { _context = context; }

        // ==========================================
        // 1. LẤY DANH SÁCH YÊU CẦU
        // (Bỏ [AdminOnly] để mọi người đều có thể đếm được số lượng thông báo ở góc phải màn hình)
        // ==========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActionRequest>>> GetRequests()
        {
            return await _context.ActionRequests
                .OrderBy(r => r.Status == "Pending" ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // ==========================================
        // 2. GỬI YÊU CẦU XÓA (Ai cũng được gửi)
        // ==========================================
        [HttpPost("request-delete")]
        public async Task<IActionResult> RequestDelete([FromBody] ActionRequest request)
        {
            request.Status = "Pending";
            request.CreatedAt = DateTime.Now;

            request.CreatedBy = HttpContext.Session.GetString("UserEmail") ?? "Staff";

            _context.ActionRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã gửi yêu cầu xóa. Vui lòng đợi Admin phê duyệt!" });
        }

        // ==========================================
        // 3. ADMIN PHÊ DUYỆT (Chỉ Admin mới được bấm duyệt)
        // ==========================================
        [HttpPost("approve/{id}")]
        [AdminOnly] // 👈 Khóa chặn ở đây: Chỉ Admin mới được thực hiện hành động này
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await _context.ActionRequests.FindAsync(id);
            if (request == null || request.Status != "Pending")
                return BadRequest(new { message = "Yêu cầu không hợp lệ hoặc đã xử lý." });

            if (request.ActionType == "Delete_Customer")
            {
                var customer = await _context.Customers.FindAsync(request.TargetId);
                if (customer != null) _context.Customers.Remove(customer);
            }

            else if (request.ActionType == "Delete_InventoryTicket")
            {
                var ticket = await _context.InventoryTickets
                    .Include(t => t.TicketDetails)
                    .FirstOrDefaultAsync(t => t.Id == request.TargetId);

                if (ticket != null)
                {
                    foreach (var detail in ticket.TicketDetails)
                    {
                        var product = await _context.Products.FindAsync(detail.ProductId);
                        if (product != null)
                        {
                            int beforeQty = product.Quantity ?? 0;
                            int changeQty = detail.Quantity;

                            if (ticket.Type.ToLower() == "nhập")
                            {
                                product.Quantity -= changeQty; // Hủy nhập -> Trừ kho
                                changeQty = -changeQty;
                            }
                            else if (ticket.Type.ToLower() == "xuất")
                            {
                                product.Quantity += changeQty; // Hủy xuất -> Cộng kho lại
                            }

                            // Ghi Thẻ Kho tự động
                            var stockCard = new StockCard
                            {
                                ProductId = product.Id,
                                TransactionDate = DateTime.Now,
                                ReferenceCode = "HUY-" + ticket.TicketCode,
                                BeforeQty = beforeQty,
                                ChangeQty = changeQty,
                                AfterQty = product.Quantity ?? 0,
                                Note = $"Duyệt hủy phiếu từ yêu cầu của {request.CreatedBy}. Lý do: {request.Reason}"
                            };
                            _context.StockCards.Add(stockCard);
                        }
                    }
                    _context.InventoryTickets.Remove(ticket);
                }
            }

            else if (request.ActionType == "Delete_Warehouse")
            {
                var warehouse = await _context.Warehouses.FindAsync(request.TargetId);
                if (warehouse != null) _context.Warehouses.Remove(warehouse);
            }

            request.Status = "Approved";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã phê duyệt và thực hiện xóa dữ liệu thành công!" });
        }

        // ==========================================
        // 4. ADMIN TỪ CHỐI (Chỉ Admin mới được bấm từ chối)
        // ==========================================
        [HttpPost("reject/{id}")]
        [AdminOnly] // 👈 Khóa chặn ở đây
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await _context.ActionRequests.FindAsync(id);
            if (request == null || request.Status != "Pending")
                return BadRequest(new { message = "Yêu cầu không hợp lệ." });

            request.Status = "Rejected";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã từ chối yêu cầu." });
        }
    }
}