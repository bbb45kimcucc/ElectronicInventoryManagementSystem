using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public WarehousesController(AppDbContext context) { _context = context; }

        // 1. Lấy danh sách tất cả các kho
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
        {
            return await _context.Warehouses.ToListAsync();
        }

        // 2. Lấy chi tiết 1 kho
        [HttpGet("{id}")]
        public async Task<ActionResult<Warehouse>> GetWarehouse(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            return warehouse == null ? NotFound(new { message = "Không tìm thấy kho hàng." }) : warehouse;
        }

        // 3. Thêm kho mới
        [HttpPost]
        public async Task<ActionResult<Warehouse>> PostWarehouse(Warehouse warehouse)
        {
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, warehouse);
        }

        // 4. Sửa thông tin kho (Đổi tên, địa chỉ...)
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
                if (!_context.Warehouses.Any(e => e.Id == id)) return NotFound(new { message = "Kho không tồn tại." });
                throw;
            }

            return NoContent();
        }

        // 5. NGHIỆP VỤ: Xóa kho an toàn (Cấm xóa nếu kho đang có linh kiện)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound(new { message = "Không tìm thấy kho để xóa." });

            // Ràng buộc: Có linh kiện nào đang nằm trong kho này không?
            var hasProducts = await _context.Products.AnyAsync(p => p.WarehouseId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = "Cảnh báo: Không thể xóa! Kho này đang chứa linh kiện. Vui lòng xuất hết hàng hoặc chuyển sang kho khác trước khi xóa." });
            }

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa kho hàng thành công." });
        }
    }
}