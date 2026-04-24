using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Helpers;
using ElectronicInventoryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BrandsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách Brand
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Brand>>> GetBrands()
        {
            return await _context.Brands.ToListAsync();
        }

        // 2. Lấy chi tiết 1 Brand
        [HttpGet("{id}")]
        public async Task<ActionResult<Brand>> GetBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return brand;
        }

        // 3. Thêm mới Brand
        [AdminOnly]
        [HttpPost]
        public async Task<ActionResult<Brand>> PostBrand(Brand brand)
        {
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }

        // 4. Cập nhật Brand
        [AdminOnly]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBrand(int id, Brand brand)
        {
            if (id != brand.Id) return BadRequest();
            _context.Entry(brand).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Brands.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        // 5. XÓA BRAND 
        [AdminOnly]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound(new { message = "Không tìm thấy thương hiệu này." });
            }

            var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);

            if (hasProducts)
            {
                return BadRequest(new
                {
                    message = "Không thể xóa thương hiệu này vì đang có sản phẩm thuộc về nó. Hãy xóa sản phẩm trước!"
                });
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return NoContent(); 
        }
    }
}