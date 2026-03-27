using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy tất cả danh mục
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        // 2. Lấy 1 danh mục theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            return category == null ? NotFound(new { message = "Không tìm thấy danh mục này." }) : category;
        }

        // 3. Thêm mới danh mục
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        // 4. Sửa danh mục (Mới thêm)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.Id) return BadRequest(new { message = "ID không khớp." });

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Categories.Any(e => e.Id == id)) return NotFound(new { message = "Danh mục không tồn tại." });
                throw;
            }

            return NoContent();
        }

        // 5. XÓA AN TOÀN (Nghiệp vụ quan trọng nhất)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound(new { message = "Không tìm thấy danh mục để xóa." });

            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);

            if (hasProducts)
            {
      
                return BadRequest(new { message = "Cảnh báo: Không thể xóa vì danh mục này đang chứa linh kiện! Hãy chuyển linh kiện sang danh mục khác trước." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa danh mục thành công." });
        }
    }
}