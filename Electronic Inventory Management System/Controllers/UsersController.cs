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

        // 1. Lấy danh sách nhân viên (Chỉ Admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            if (!AuthHelper.IsAdmin())
            {
                return Unauthorized(new { message = "Chỉ Admin mới được xem danh sách!" });
            }

            return await _context.Users.ToListAsync();
        }

        // 2. Lấy chi tiết 1 nhân viên (Chỉ Admin)
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            if (!AuthHelper.IsAdmin())
            {
                return Unauthorized(new { message = "Chỉ Admin mới được xem!" });
            }

            var user = await _context.Users.FindAsync(id);

            return user == null
                ? NotFound(new { message = "Không tìm thấy người dùng này." })
                : user;
        }

        // 3. Thêm nhân viên (Chỉ Admin)
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (!AuthHelper.IsAdmin())
            {
                return Unauthorized(new { message = "Chỉ Admin mới được tạo user!" });
            }

            // Check email tồn tại
            var isExist = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (isExist)
            {
                return BadRequest(new { message = "Email đã được sử dụng!" });
            }

            // Validate role
            if (user.Role != "Admin" && user.Role != "Staff")
            {
                return BadRequest(new { message = "Role không hợp lệ!" });
            }

            // Default role
            if (string.IsNullOrEmpty(user.Role))
            {
                user.Role = "Staff";
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // 4. Cập nhật (Chỉ Admin)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (!AuthHelper.IsAdmin())
            {
                return Unauthorized(new { message = "Chỉ Admin mới được cập nhật!" });
            }

            if (id != user.Id)
            {
                return BadRequest(new { message = "ID không khớp." });
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id))
                {
                    return NotFound(new { message = "Người dùng không tồn tại." });
                }

                throw;
            }

            return NoContent();
        }

        // 5. Xóa (Chỉ Admin)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!AuthHelper.IsAdmin())
            {
                return Unauthorized(new { message = "Chỉ Admin mới được xóa!" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            var hasCreatedTickets = await _context.InventoryTickets
                .AnyAsync(t => t.UserId == id);

            if (hasCreatedTickets)
            {
                return BadRequest(new
                {
                    message = "Cấm xóa! Nhân viên đã có lịch sử phiếu."
                });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa thành công." });
        }

        // 6. Đăng nhập
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ!" });
            }

            // ⚠️ TODO: nên mã hóa password bằng BCrypt
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });
            }

            // 👉 Lưu role
            AuthHelper.CurrentRole = user.Role;

            user.Password = "********";

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                data = user
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}