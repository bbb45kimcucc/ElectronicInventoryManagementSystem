using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context) { _context = context; }

        // 1. Lấy danh sách nhân viên
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // 2. Lấy chi tiết 1 nhân viên
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            return user == null ? NotFound(new { message = "Không tìm thấy người dùng này." }) : user;
        }

        // 3. THÊM NHÂN VIÊN (Kiểm tra trùng lặp)
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            // Nghiệp vụ: Kiểm tra xem Email hoặc Username đã có ai xài chưa (Giả sử model má có cột Username hoặc Email)
            // Nếu Model của má tên cột khác thì tự đổi lại cho khớp nha (vd: user.Email, user.TaiKhoan...)
            var isExist = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (isExist)
            {
                return BadRequest(new { message = "Email này đã được sử dụng. Vui lòng chọn Email khác!" });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // 4. CẬP NHẬT THÔNG TIN NHÂN VIÊN
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id) return BadRequest(new { message = "ID không khớp." });

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id)) return NotFound(new { message = "Người dùng không tồn tại." });
                throw;
            }

            return NoContent();
        }

        // 5. XÓA AN TOÀN (Bảo vệ lịch sử hệ thống)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            // RÀNG BUỘC: Nhân viên này đã từng lập phiếu nào chưa?
            var hasCreatedTickets = await _context.InventoryTickets.AnyAsync(t => t.UserId == id);
            if (hasCreatedTickets)
            {
                return BadRequest(new { message = "Cấm xóa! Nhân viên này đã từng lập phiếu kho. Chỉ được phép khóa tài khoản (đổi trạng thái) để giữ lại lịch sử!" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa nhân viên thành công." });
        }

        // 6. NGHIỆP VỤ ĐĂNG NHẬP (Dành cho Frontend gọi vào)
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ Email và Mật khẩu." });
            }

            // Tìm user có Email và Password khớp với DB (Lưu ý: Thực tế pass phải được mã hóa)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Email hoặc Mật khẩu không chính xác!" });
            }

            // Trả về thông tin user (Nhưng TUYỆT ĐỐI che cái Password lại không cho Frontend thấy)
            user.Password = "********";

            return Ok(new { message = "Đăng nhập thành công!", data = user });
        }
    }

    // Class phụ trợ để nhận cục dữ liệu Đăng nhập từ React/Vue gửi lên
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}