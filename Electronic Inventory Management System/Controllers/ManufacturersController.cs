using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManufacturersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ManufacturersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy tất cả Hãng
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Manufacturer>>> GetManufacturers()
        {
            return await _context.Manufacturers.ToListAsync();
        }

        // 2. Lấy 1 Hãng theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Manufacturer>> GetManufacturer(int id)
        {
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            return manufacturer == null ? NotFound(new { message = "Không tìm thấy Hãng." }) : manufacturer;
        }

        // 3. Thêm mới Hãng
        [HttpPost]
        public async Task<ActionResult<Manufacturer>> PostManufacturer(Manufacturer manufacturer)
        {
            _context.Manufacturers.Add(manufacturer);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetManufacturer), new { id = manufacturer.Id }, manufacturer);
        }

        // 4. Sửa Hãng
        [HttpPut("{id}")]
        public async Task<IActionResult> PutManufacturer(int id, Manufacturer manufacturer)
        {
            if (id != manufacturer.Id) return BadRequest(new { message = "ID không khớp." });
            _context.Entry(manufacturer).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. XÓA AN TOÀN (Cực kỳ quan trọng)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteManufacturer(int id)
        {
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null) return NotFound(new { message = "Không tìm thấy Hãng." });

            // Kiểm tra xem có linh kiện nào đang xài Hãng này không?
            var hasProducts = await _context.Products.AnyAsync(p => p.ManufacturerId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = "CẢNH BÁO: Đang có linh kiện thuộc Hãng này. Hãy đổi hãng cho linh kiện trước khi xóa!" });
            }

            _context.Manufacturers.Remove(manufacturer);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa Hãng thành công." });
        }
    }
}