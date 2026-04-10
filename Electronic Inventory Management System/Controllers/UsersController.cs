using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;
using ElectronicInventoryManagementSystem.Helpers;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách nhân viên
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            // Soi Header của yêu cầu hiện tại
            if (!AuthHelper.IsAdmin(Request))
            {
                return Unauthorized(new { message = "Lỗi 401: Backend không thấy thẻ Admin của bạn!" });
            }

            return await _context.Users.ToListAsync();
        }

        // 2. Lấy chi tiết 1 nhân viên
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            if (!AuthHelper.IsAdmin(Request))
            {
                return Unauthorized(new { message = "Bạn không có quyền xem chi tiết!" });
            }

            var user = await _context.Users.FindAsync(id);
            return user == null ? NotFound(new { message = "Không tìm thấy!" }) : user;
        }

        // 3. Thêm nhân viên mới
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (!AuthHelper.IsAdmin(Request))
            {
                return Unauthorized(new { message = "Chỉ Admin mới được tạo nhân viên mới!" });
            }

            // Check email tồn tại
            var isExist = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (isExist) return BadRequest(new { message = "Email đã được sử dụng!" });

            // Lưu User (Giữ nguyên logic của Cúc)
            if (string.IsNullOrEmpty(user.Role)) user.Role = "Staff";
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // 4. Cập nhật nhân viên
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (!AuthHelper.IsAdmin(Request))
            {
                return Unauthorized(new { message = "Không có quyền cập nhật!" });
            }

            if (id != user.Id) return BadRequest(new { message = "ID không khớp." });
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. Xóa nhân viên
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!AuthHelper.IsAdmin(Request))
            {
                return Unauthorized(new { message = "Chỉ Admin mới được xóa!" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Check ràng buộc xóa (Giữ nguyên logic xịn của Cúc)
            var hasCreatedTickets = await _context.InventoryTickets.AnyAsync(t => t.UserId == id);
            if (hasCreatedTickets) return BadRequest(new { message = "Cấm xóa! Nhân viên đã có lịch sử làm việc." });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa thành công." });
        }

        // 6. Đăng nhập (Giữ nguyên để lấy dữ liệu ban đầu)
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu rồi Cúc ơi!" });
            }

            // --- ĐÂY LÀ PHẦN CẦN BỔ SUNG ---
            // Ghi vào "sổ tay" Session để Backend tự nhớ mặt
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);
            // -------------------------------

            user.Password = "********"; 

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                data = user
            });
        }
    }

    public class LoginRequest { public string Email { get; set; } public string Password { get; set; } }
}