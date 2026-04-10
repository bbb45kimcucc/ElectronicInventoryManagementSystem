using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách toàn bộ Đơn hàng
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product) // Móc luôn tên món hàng trong chi tiết
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // 2. Lấy chi tiết 1 Đơn hàng
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng này." });
            return order;
        }

        // 3. TẠO ĐƠN HÀNG MỚI (Từ giao diện người dùng gửi xuống)
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            if (order.OrderDetails == null || !order.OrderDetails.Any())
            {
                return BadRequest(new { message = "Đơn hàng phải có ít nhất 1 sản phẩm bên trong." });
            }

            // Tự động tính toán Tổng tiền (An toàn hơn để Frontend tự tính)
            decimal total = 0;
            foreach (var detail in order.OrderDetails)
            {
                total += (detail.Quantity * detail.UnitPrice);
            }
            order.TotalAmount = total;
            order.OrderDate = DateTime.Now;

            // Tự sinh mã Vận đơn (Tracking ID) ngẫu nhiên nếu chưa có
            if (string.IsNullOrEmpty(order.TrackingId))
            {
                order.TrackingId = "TRK" + DateTime.Now.Ticks.ToString().Substring(8);
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        // 4. ĐỔI TRẠNG THÁI ĐƠN HÀNG (API đặc biệt cho cái Dropdown React)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            // Cập nhật trạng thái mới
            order.Status = newStatus;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật trạng thái thành công!", status = order.Status });
        }

        // 5. Xóa đơn hàng
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa đơn hàng thành công." });
        }
    }
}