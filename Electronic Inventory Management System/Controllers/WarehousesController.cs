using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Helpers; // Dùng AdminOnly
using ElectronicInventoryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // 👈 THÊM DÒNG NÀY ĐỂ HẾT BÁO ĐỎ CHỖ [Authorize]

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải đăng nhập mới được vào
    public class WarehousesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public WarehousesController(AppDbContext context) { _context = context; }

        // ==========================================
        // QUYỀN CHUNG: AI CŨNG ĐƯỢC XEM (STAFF & ADMIN)
        // ==========================================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
        {
            return await _context.Warehouses.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Warehouse>> GetWarehouse(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound(new { message = "Không tìm thấy kho hàng." });
            return warehouse;
        }

        // ==========================================
        // QUYỀN RIÊNG: CHỈ ADMIN MỚI ĐƯỢC THÊM, SỬA, XÓA
        // ==========================================

        // 3. Thêm kho mới
        [AdminOnly] // 👈 Gắn khóa vô nè
        [HttpPost]
        public async Task<ActionResult<Warehouse>> PostWarehouse(Warehouse warehouse)
        {
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, warehouse);
        }

        // 4. Sửa thông tin kho
        [AdminOnly] // 👈 Gắn khóa vô nè
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWarehouse(int id, Warehouse warehouse)
        {
            if (id != warehouse.Id) return BadRequest(new { message = "ID không khớp." });

            _context.Entry(warehouse).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Warehouses.Any(e => e.Id == id))
                    return NotFound(new { message = "Kho không tồn tại." });
                throw;
            }

            return NoContent();
        }

        // 5. Xóa kho an toàn
        [AdminOnly] // 👈 Gắn khóa vô nè
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound(new { message = "Không tìm thấy kho để xóa." });

            var hasProducts = await _context.Products.AnyAsync(p => p.WarehouseId == id);
            if (hasProducts)
            {
                return BadRequest(new
                {
                    message = "Cấm xóa! Kho này đang chứa linh kiện. Hãy chuyển hết hàng sang kho khác trước khi xóa."
                });
            }

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa kho hàng thành công." });
        }
    }
}