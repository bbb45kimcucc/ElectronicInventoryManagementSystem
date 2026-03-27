using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SuppliersController(AppDbContext context) { _context = context; }

        // 1. Lấy danh sách Nhà cung cấp
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            return await _context.Suppliers.ToListAsync();
        }

        // 2. Tra cứu chi tiết 1 Nhà cung cấp
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            return supplier == null ? NotFound(new { message = "Không tìm thấy nhà cung cấp này." }) : supplier;
        }

        // 3. Nghiệp vụ Tìm kiếm nhanh (Theo Tên, Điện thoại, hoặc Email)
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Supplier>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query)) return await GetSuppliers();

            // Lọc theo tên hoặc sđt (Nếu model của má có cột khác thì đổi lại xíu nha)
            return await _context.Suppliers
                .Where(s => s.Name.Contains(query) || s.Phone.Contains(query) || s.Email.Contains(query))
                .ToListAsync();
        }

        // 4. Nghiệp vụ Xem lịch sử nhập hàng của Nhà cung cấp này
        [HttpGet("{id}/tickets")]
        public async Task<ActionResult<IEnumerable<InventoryTicket>>> GetSupplierTickets(int id)
        {
            var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == id);
            if (!supplierExists) return NotFound(new { message = "Nhà cung cấp không tồn tại." });

            var tickets = await _context.InventoryTickets
                .Where(t => t.SupplierId == id)
                .Include(t => t.TicketDetails)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tickets);
        }

        // 5. Thêm Nhà cung cấp mới
        [HttpPost]
        public async Task<ActionResult<Supplier>> PostSupplier(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }

        // 6. Sửa thông tin Nhà cung cấp
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSupplier(int id, Supplier supplier)
        {
            if (id != supplier.Id) return BadRequest(new { message = "ID không khớp." });

            _context.Entry(supplier).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Suppliers.Any(e => e.Id == id)) return NotFound(new { message = "Nhà cung cấp không tồn tại." });
                throw;
            }

            return NoContent();
        }

        // 7. NGHIỆP VỤ XÓA AN TOÀN (Bảo vệ dữ liệu)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound(new { message = "Không tìm thấy nhà cung cấp." });

            // Kiểm tra xem đã từng nhập hàng của ông này chưa?
            var hasTickets = await _context.InventoryTickets.AnyAsync(t => t.SupplierId == id);
            if (hasTickets)
            {
                return BadRequest(new { message = "Cấm xóa! Bạn đã từng nhập hàng từ Nhà cung cấp này. Để đảm bảo dữ liệu kế toán, chỉ được phép sửa thông tin." });
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa nhà cung cấp thành công." });
        }
    }
}