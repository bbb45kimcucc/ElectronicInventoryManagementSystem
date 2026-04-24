using ClosedXML.Excel;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Helpers;
using ElectronicInventoryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // 2. TÌM KIẾM: Đã thêm tìm theo Phương thức thanh toán
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Customer>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query)) return await GetCustomers();

            return await _context.Customers
                .Where(c => c.Name.Contains(query)
                         || c.Phone.Contains(query)
                         || (c.PaymentMethod != null && c.PaymentMethod.Contains(query))) // Thêm dòng này
                .ToListAsync();
        }

        // 3. Lấy chi tiết 1 khách hàng
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            return customer == null ? NotFound(new { message = "Không tìm thấy khách hàng này." }) : customer;
        }

        // 4. Xem lịch sử phiếu xuất (Giữ nguyên logic xịn của Cúc)
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

        // 5. THÊM MỚI: Xử lý giá trị mặc định cho PaymentMethod
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            // Nếu Frontend không gửi lên, mặc định là Tiền mặt
            if (string.IsNullOrEmpty(customer.PaymentMethod))
            {
                customer.PaymentMethod = "Tiền mặt";
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

        // 6. CẬP NHẬT
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

        // 7. XÓA (Giữ nguyên ràng buộc lịch sử giao dịch)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(new { message = "Không tìm thấy khách hàng để xóa." });

            var hasTickets = await _context.InventoryTickets.AnyAsync(t => t.CustomerId == id);
            if (hasTickets)
            {
                return BadRequest(new { message = "Cấm xóa! Khách hàng này đã có lịch sử giao dịch." });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa khách hàng thành công." });
        }
        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel()
        {
            var customers = await _context.Customers.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("DS_KhachHang");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "Mã KH";
                worksheet.Cell(currentRow, 2).Value = "Tên Khách Hàng";
                worksheet.Cell(currentRow, 3).Value = "Số Điện Thoại";
                worksheet.Cell(currentRow, 4).Value = "Thanh Toán Ưu Tiên";

                worksheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                worksheet.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var cus in customers)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = cus.Id;
                    worksheet.Cell(currentRow, 2).Value = cus.Name;
                    worksheet.Cell(currentRow, 3).Value = cus.Phone;
                    worksheet.Cell(currentRow, 4).Value = cus.PaymentMethod;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KhachHang_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
        }
    }
}