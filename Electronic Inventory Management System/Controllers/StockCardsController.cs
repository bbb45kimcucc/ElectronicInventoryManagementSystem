using ClosedXML.Excel;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockCardsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public StockCardsController(AppDbContext context) { _context = context; }

        // 1. Xem tất cả (Có phân trang cho khỏi lag)
        [HttpGet]
        public async Task<ActionResult> GetStockCards([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var totalItems = await _context.StockCards.CountAsync();
            var cards = await _context.StockCards
                .Include(s => s.Product)
                .OrderByDescending(s => s.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Data = cards
            });
        }

        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<StockCard>>> GetByProduct(
            int productId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.StockCards.Where(s => s.ProductId == productId).AsQueryable();

            // Lọc từ ngày
            if (startDate.HasValue)
            {
                query = query.Where(s => s.TransactionDate >= startDate.Value);
            }

            // Lọc đến ngày
            if (endDate.HasValue)
            {
                query = query.Where(s => s.TransactionDate <= endDate.Value);
            }

            var result = await query
                .OrderByDescending(s => s.TransactionDate)
                .ToListAsync();

            return Ok(result);
        }

        // 3. Thêm mới thẻ kho (Dùng khi kiểm kê kho hoặc nhập đầu kỳ)
        [HttpPost]
        public async Task<ActionResult<StockCard>> PostStockCard(StockCard stockCard)
        {
            stockCard.TransactionDate = DateTime.Now;

            _context.StockCards.Add(stockCard);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã ghi nhận lịch sử vào Thẻ kho thành công!", data = stockCard });
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel()
        {
            var cards = await _context.StockCards
                .Include(s => s.Product) // Kéo theo tên Sản phẩm cho dễ đọc
                .OrderByDescending(s => s.TransactionDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("TheKho");
                var currentRow = 1;

                // Cập nhật lại Tiêu đề cho đúng với Model siêu xịn của Cúc
                worksheet.Cell(currentRow, 1).Value = "Mã Thẻ";
                worksheet.Cell(currentRow, 2).Value = "Tên Sản Phẩm";
                worksheet.Cell(currentRow, 3).Value = "Ngày Giao Dịch";
                worksheet.Cell(currentRow, 4).Value = "Mã Phiếu Gốc"; // ReferenceCode
                worksheet.Cell(currentRow, 5).Value = "Tồn Đầu";      // BeforeQty
                worksheet.Cell(currentRow, 6).Value = "SL Thay Đổi";   // ChangeQty
                worksheet.Cell(currentRow, 7).Value = "Tồn Cuối";      // AfterQty
                worksheet.Cell(currentRow, 8).Value = "Ghi Chú";       // Note

                var header = worksheet.Range(1, 1, 1, 8);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var card in cards)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = card.Id;
                    worksheet.Cell(currentRow, 2).Value = card.Product?.Name ?? "N/A";
                    worksheet.Cell(currentRow, 3).Value = card.TransactionDate.ToString("dd/MM/yyyy HH:mm");

                    // Map đúng tên các cột trong file StockCard.cs của Cúc
                    worksheet.Cell(currentRow, 4).Value = card.ReferenceCode;
                    worksheet.Cell(currentRow, 5).Value = card.BeforeQty;

                    // Xử lý logic hiển thị (+/-) cho SL Thay Đổi dựa vào Before/After
                    string changeType = card.AfterQty >= card.BeforeQty ? $"+{card.ChangeQty}" : $"-{card.ChangeQty}";
                    worksheet.Cell(currentRow, 6).Value = changeType;

                    worksheet.Cell(currentRow, 7).Value = card.AfterQty;
                    worksheet.Cell(currentRow, 8).Value = card.Note;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TheKho_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
        }
    }
}