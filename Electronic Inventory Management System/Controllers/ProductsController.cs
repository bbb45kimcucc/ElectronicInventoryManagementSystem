using ClosedXML.Excel;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Electronic_Inventory_Management_System.Helpers;
using ElectronicInventoryManagementSystem.Data;
using ElectronicInventoryManagementSystem.Helpers;
using ElectronicInventoryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicInventoryManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProductsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var products = await _context.Products
                .Include(p => p.Category).Include(p => p.Unit)
                .Include(p => p.Brand).Include(p => p.Images)
                .ToListAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category).Include(p => p.Unit)
                .Include(p => p.Brand).Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return product;
        }
        [AdminOnly]
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(product.Image))
            {
                _context.ProductImages.Add(new ProductImage { ProductId = product.Id, ImageUrl = product.Image });
                await _context.SaveChangesAsync();
            }
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        [AdminOnly]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null) return NotFound();

            // Chỉ cập nhật những thứ cho phép sửa, giữ nguyên Quantity (Tồn kho)
            existingProduct.Name = product.Name;
            existingProduct.SKU = product.SKU;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.BrandId = product.BrandId;
            existingProduct.UnitId = product.UnitId;
            existingProduct.AveragePrice = product.AveragePrice;

            if (!string.IsNullOrEmpty(product.Image))
            {
                var oldImgs = _context.ProductImages.Where(img => img.ProductId == id);
                _context.ProductImages.RemoveRange(oldImgs);
                _context.ProductImages.Add(new ProductImage { ProductId = id, ImageUrl = product.Image });
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        [AdminOnly]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa sản phẩm thành công." });
        }

        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")] // FIX LỖI SWAGGER TẠI ĐÂY
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Chưa có file ảnh!");

            var config = new CloudinarySettings();
            var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
            var cloudinary = new Cloudinary(account);

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "LinhKien_WMS",
                Transformation = new Transformation().Width(500).Height(500).Crop("fill")
            };
            var uploadResult = await cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null) return BadRequest(uploadResult.Error.Message);
            return Ok(new { url = uploadResult.SecureUrl.ToString() });
        }
        [AdminOnly]
        [HttpGet("export-excel")]

        public async Task<IActionResult> ExportExcel()
        {
            var products = await _context.Products.Include(p => p.Category).Include(p => p.Unit).ToListAsync();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("TonKho");
            ws.Cell(1, 1).Value = "SKU"; ws.Cell(1, 2).Value = "Tên"; ws.Cell(1, 3).Value = "Tồn";

            int row = 2;
            foreach (var p in products)
            {
                ws.Cell(row, 1).Value = p.SKU;
                ws.Cell(row, 2).Value = p.Name;
                ws.Cell(row, 3).Value = p.Quantity ?? 0;
                row++;
            }
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BaoCao.xlsx");
        }
    }
}