using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CustomersController(AppDbContext context) { _context = context; }

        // 1. Lấy danh sách khách hàng
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _context.Customers.ToListAsync();
        }

        // 2. Tìm kiếm khách hàng theo Tên hoặc Số điện thoại (Nghiệp vụ cốt lõi)
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Customer>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query)) return await GetCustomers();
            return await _context.Customers
                .Where(c => c.Name.Contains(query) || c.Phone.Contains(query))
                .ToListAsync();
        }

        // 3. Lấy chi tiết 1 khách hàng
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            return customer == null ? NotFound(new { message = "Không tìm thấy khách hàng này." }) : customer;
        }

        // 4. Xem lịch sử các Phiếu Xuất của khách hàng này
        [HttpGet("{id}/tickets")]
        public async Task<ActionResult<IEnumerable<InventoryTicket>>> GetCustomerTickets(int id)
        {
            var customerExists = await _context.Customers.AnyAsync(c => c.Id == id);
            if (!customerExists) return NotFound(new { message = "Khách hàng không tồn tại." });

            var tickets = await _context.InventoryTickets
                .Where(t => t.CustomerId == id)
                .Include(t => t.TicketDetails)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tickets);
        }

        // 5. Thêm khách hàng mới
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

        // 6. Cập nhật thông tin khách hàng (Sửa SĐT, Địa chỉ...)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.Id) return BadRequest(new { message = "ID không khớp." });

            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Customers.Any(e => e.Id == id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // 7. Xóa an toàn (Nghiệp vụ ràng buộc)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(new { message = "Không tìm thấy khách hàng để xóa." });

            var hasTickets = await _context.InventoryTickets.AnyAsync(t => t.CustomerId == id);
            if (hasTickets)
            {
                return BadRequest(new { message = "Cấm xóa! Khách hàng này đã có lịch sử giao dịch. Chỉ có thể sửa thông tin." });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa khách hàng thành công." });
        }
    }
}