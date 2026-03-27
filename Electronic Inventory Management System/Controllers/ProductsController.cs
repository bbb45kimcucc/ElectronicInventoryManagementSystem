using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy tất cả (GetAll)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Warehouse)
                .ToListAsync();
        }

        // 2. Phân trang (GetPaged)
        [HttpGet("paged")]
        public async Task<ActionResult> GetPagedProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var totalItems = await _context.Products.CountAsync();
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Warehouse)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Data = products
            });
        }

        // 3. Lấy theo ID (GetById)
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(string id)
        {
            if (!int.TryParse(id, out int productId)) return BadRequest(new { message = "ID phải là số." });

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm." });
            return product;
        }

        // 4. Lọc theo Category
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetByCategoryId(int categoryId)
        {
            return await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .ToListAsync();
        }

        // 5. Lọc theo Warehouse
        [HttpGet("warehouse/{warehouseId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetByWarehouseId(int warehouseId)
        {
            return await _context.Products
                .Where(p => p.WarehouseId == warehouseId)
                .Include(p => p.Warehouse)
                .ToListAsync();
        }

        // 6. Tìm kiếm (Search)
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query)) return await GetAll();

            return await _context.Products
                .Where(p => p.Name.Contains(query))
                .Include(p => p.Category)
                .ToListAsync();
        }

        // 7. Thêm mới (Create)
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = product.Id.ToString() }, product);
        }

        // 8. Cập nhật (Update)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id) return BadRequest(new { message = "ID không khớp." });

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == id)) return NotFound(new { message = "Sản phẩm không tồn tại." });
                throw;
            }

            return NoContent();
        }

        // 9. Xóa nhiều (Bulk Delete)
        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteMultiple([FromQuery] string ids)
        {
            if (string.IsNullOrEmpty(ids)) return BadRequest(new { message = "Chưa chọn sản phẩm nào để xóa." });

            var idList = ids.Split(',')
                            .Select(s => int.TryParse(s.Trim(), out var i) ? i : (int?)null)
                            .Where(i => i.HasValue)
                            .Select(i => i.Value)
                            .ToList();

            if (!idList.Any()) return BadRequest(new { message = "Danh sách ID không hợp lệ." });

            var productsToDelete = await _context.Products
                .Where(p => idList.Contains(p.Id))
                .ToListAsync();

            if (!productsToDelete.Any()) return NotFound(new { message = "Không tìm thấy dữ liệu khớp để xóa." });

            _context.Products.RemoveRange(productsToDelete);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã dọn dẹp thành công {productsToDelete.Count} sản phẩm." });
        }

        // 10. Xóa một (Single Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            if (!int.TryParse(id, out int productId)) return BadRequest(new { message = "ID phải là số." });

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa sản phẩm thành công." });
        }
    }
}