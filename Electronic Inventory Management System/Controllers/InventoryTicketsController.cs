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
    public class InventoryTicketsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public InventoryTicketsController(AppDbContext context) { _context = context; }

        // 1. Lấy danh sách phiếu 
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryTicket>>> GetTickets()
        {
            return await _context.InventoryTickets
                .Include(t => t.User)
                .Include(t => t.Customer)
                .Include(t => t.Supplier)
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Product) // Lấy luôn tên sản phẩm trong chi tiết
                .OrderByDescending(t => t.CreatedAt) // Phiếu mới nhất lên đầu
                .ToListAsync();
        }

        // 2. Lấy chi tiết 1 phiếu
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryTicket>> GetTicket(int id)
        {
            var ticket = await _context.InventoryTickets
                .Include(t => t.User)
                .Include(t => t.Customer)
                .Include(t => t.Supplier)
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound(new { message = "Không tìm thấy phiếu này." });
            return ticket;
        }


        [HttpPost]
        public async Task<ActionResult<InventoryTicket>> PostTicket(InventoryTicket ticket)
        {
            if (ticket.TicketDetails == null || !ticket.TicketDetails.Any())
            {
                return BadRequest(new { message = "Phiếu phải có ít nhất 1 sản phẩm bên trong." });
            }

            // 1. Lưu Phiếu Kho trước để nó lấy ID (Cần ID để làm Khóa chính)
            ticket.CreatedAt = DateTime.Now;
            _context.InventoryTickets.Add(ticket);

            // Phải SaveChanges 1 lần ở đây để ticket có cái ID thật trong Database
            await _context.SaveChangesAsync();

            // 2. Lặp qua từng chi tiết hàng hóa để xử lý tồn kho và Thẻ kho
            foreach (var detail in ticket.TicketDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product == null)
                    return BadRequest(new { message = $"Sản phẩm có ID {detail.ProductId} không tồn tại." });

                int beforeQty = product.Quantity ?? 0; int changeQty = detail.Quantity;

                if (ticket.Type.ToLower() == "nhập")
                {
                    product.Quantity += changeQty; // Nhập thì cộng kho
                }
                else if (ticket.Type.ToLower() == "xuất")
                {
                    if (product.Quantity < changeQty)
                    {
                        _context.InventoryTickets.Remove(ticket);
                        await _context.SaveChangesAsync();
                        return BadRequest(new { message = $"Sản phẩm '{product.Name}' không đủ số lượng để xuất. Tồn: {product.Quantity}" });
                    }
                    product.Quantity -= changeQty; // Xuất thì trừ kho
                    changeQty = -changeQty; // Đổi dấu thành số âm để ghi vào Thẻ kho cho chuẩn
                }

                var stockCard = new StockCard
                {
                    ProductId = product.Id,
                    TransactionDate = DateTime.Now,
                    ReferenceCode = ticket.TicketCode,
                    BeforeQty = beforeQty,
                    ChangeQty = changeQty,
                    AfterQty = product.Quantity ?? 0,
                    Note = $"Lập phiếu {ticket.Type.ToLower()} tự động"
                };
                _context.StockCards.Add(stockCard);
            }

            // Lưu một lần cuối cùng (Gồm Tồn kho mới + Danh sách Thẻ kho)
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id, [FromQuery] string? reason) // <-- THÊM [FromQuery] string? reason Ở ĐÂY
        {
            var ticket = await _context.InventoryTickets
                .Include(t => t.TicketDetails)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound(new { message = "Không tìm thấy phiếu." });

            // Trả lại kho trước khi xóa phiếu
            foreach (var detail in ticket.TicketDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    int beforeQty = product.Quantity ?? 0;
                    int changeQty = detail.Quantity;

                    if (ticket.Type.ToLower() == "nhập")
                    {
                        product.Quantity -= changeQty; // Xóa phiếu Nhập -> Bị trừ kho
                        changeQty = -changeQty; // Ghi số âm
                    }
                    else if (ticket.Type.ToLower() == "xuất")
                    {
                        product.Quantity += changeQty; // Xóa phiếu Xuất -> Được cộng kho lại
                    }

                    // Ghi nhận lịch sử hoàn tác vào Thẻ kho (KÈM LÝ DO CỦA NHÂN VIÊN)
                    string cancelNote = string.IsNullOrWhiteSpace(reason)
                        ? $"Hủy phiếu {ticket.Type.ToLower()} - Hoàn lại kho"
                        : $"Hủy phiếu {ticket.Type.ToLower()} - Lý do: {reason}"; // <-- ĐƯA LÝ DO VÀO ĐÂY

                    var stockCard = new StockCard
                    {
                        ProductId = product.Id,
                        TransactionDate = DateTime.Now,
                        ReferenceCode = "HỦY-" + ticket.TicketCode,
                        BeforeQty = beforeQty,
                        ChangeQty = changeQty,
                        AfterQty = product.Quantity ?? 0,
                        Note = cancelNote  // <-- LƯU VÀO DB
                    };
                    _context.StockCards.Add(stockCard);
                }
            }

            _context.InventoryTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã hủy phiếu, hoàn tồn kho và ghi nhận lý do thành công." });
        }

        [AdminOnly]
        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel()
        {
            // 1. QUAN TRỌNG: Phải có .Include(t => t.User) thì mới lấy được Tên nhân viên
            var tickets = await _context.InventoryTickets
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("LichSuNhapXuat");
                var currentRow = 1;

                // 2. Sửa lại tiêu đề cột cho chuyên nghiệp
                worksheet.Cell(currentRow, 1).Value = "Mã Phiếu";
                worksheet.Cell(currentRow, 2).Value = "Loại Phiếu";
                worksheet.Cell(currentRow, 3).Value = "Ngày Lập";
                worksheet.Cell(currentRow, 4).Value = "Người Lập (Nhân Viên)"; // Đổi tên tiêu đề
                worksheet.Cell(currentRow, 5).Value = "Tổng Số Lượng";
                worksheet.Cell(currentRow, 6).Value = "Tổng Tiền (VNĐ)";

                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var ticket in tickets)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = ticket.TicketCode;

                    // Tự động chuyển tên Loại phiếu cho đẹp
                    string typeName = ticket.Type.ToLower() == "nhập" ? "Nhập Kho" : "Xuất Kho";
                    worksheet.Cell(currentRow, 2).Value = typeName;

                    worksheet.Cell(currentRow, 3).Value = ticket.CreatedAt.ToString("dd/MM/yyyy HH:mm");

                    // 3. THAY ĐỔI Ở ĐÂY: Lấy FullName của User thay vì UserId
                    worksheet.Cell(currentRow, 4).Value = ticket.User?.FullName ?? ticket.User?.Username ?? "---";

                    worksheet.Cell(currentRow, 5).Value = ticket.TotalQuantity;
                    worksheet.Cell(currentRow, 6).Value = ticket.TotalAmount;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoNhapXuat_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }

        }
        [AdminOnly]
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Cúc ơi, chưa chọn file Excel mà!" });

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1); // Lấy sheet đầu tiên
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua dòng tiêu đề

                    foreach (var row in rows)
                    {
                        // 1. Đọc dữ liệu từ các ô
                        var ticketCode = row.Cell(1).GetValue<string>();
                        var typeStr = row.Cell(2).GetValue<string>();
                        var createdAtStr = row.Cell(3).GetValue<string>();
                        var totalQty = row.Cell(5).GetValue<int>();
                        var totalAmt = row.Cell(6).GetValue<decimal>();

                        // 2. Kiểm tra nếu phiếu này đã tồn tại trong DB chưa (tránh trùng)
                        var exists = await _context.InventoryTickets.AnyAsync(t => t.TicketCode == ticketCode);
                        if (exists) continue;

                        // 3. Chuyển đổi dữ liệu về chuẩn DB
                        var ticket = new InventoryTicket
                        {
                            TicketCode = ticketCode,
                            Type = typeStr.Contains("Nhập") ? "Nhập" : "Xuất",
                            CreatedAt = DateTime.ParseExact(createdAtStr, "dd/MM/yyyy HH:mm", null),
                            TotalQuantity = totalQty,
                            TotalAmount = totalAmt,
                            UserId = 1 // Tạm thời gán cho Admin hoặc tìm UserId theo tên ở cột 4
                        };

                        _context.InventoryTickets.Add(ticket);
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Đã nhập dữ liệu từ Excel thành công!" });
        }
    }
}
