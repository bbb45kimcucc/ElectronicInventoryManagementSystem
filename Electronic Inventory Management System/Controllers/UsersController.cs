using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Helpers;
using ElectronicInventoryManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context) { _context = context; }

        // 1. Lấy danh sách: Chỉ Admin
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // 2. Lấy chi tiết: Admin thấy hết, Staff thấy chính mình
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var currentUserEmail = HttpContext.Session.GetString("UserEmail");
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound();

            // Check: Nếu là Admin HOẶC là chính chủ thì mới cho xem
            if (AuthHelper.IsAdmin(HttpContext) || user.Email == currentUserEmail)
            {
                user.Password = "********";
                return Ok(user);
            }

            return StatusCode(403, new { message = "Bạn không có quyền xem thông tin người khác!" });
        }

        // 3. Thêm mới: Chỉ Admin
        [HttpPost]
        [AdminOnly]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest(new { message = "Email đã tồn tại!" });

            if (string.IsNullOrEmpty(user.Role)) user.Role = "Staff";
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // 4. Cập nhật: Chỉ Admin
        [HttpPut("{id}")]
        [AdminOnly]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id) return BadRequest(new { message = "ID không khớp." });
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. Xóa: Chỉ Admin
        [HttpDelete("{id}")]
        [AdminOnly]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (await _context.InventoryTickets.AnyAsync(t => t.UserId == id))
                return BadRequest(new { message = "Không thể xóa nhân viên đã có lịch sử làm việc!" });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa thành công." });
        }

        // 6. Login (Không khóa, ai cũng phải vào được trang này)
        [HttpPost("login")]
        [AllowAnonymous] // 👈 QUAN TRỌNG: Mở cửa riêng cho hàm này để ai cũng đăng nhập được
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);

            if (user == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });

            // Vẫn giữ lại Session để code cũ của Cúc không bị lỗi
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);

            // ==========================================
            // BẮT ĐẦU IN THẺ COOKIE ĐỂ QUA CỬA [Authorize]
            // ==========================================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role) // Phân biệt Admin/Staff ở đây
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60) 
            };

            // Ép Backend phát thẻ Cookie ném về cho React
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            // ==========================================

            user.Password = "********";
            return Ok(new { message = "Đăng nhập thành công!", data = user });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear(); // Xóa Session
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Hủy Cookie
            return Ok(new { message = "Đã đăng xuất" });
        }
    }

    public class LoginRequest { public string Email { get; set; } public string Password { get; set; } }
}