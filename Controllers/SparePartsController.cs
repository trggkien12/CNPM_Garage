using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SparePartsController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public SparePartsController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet("list")]
        public async Task<IActionResult> GetSpareParts()
        {
            var parts = await _context.SpareParts
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(parts));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSparePart(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã phụ tùng không hợp lệ"));

            var part = await _context.SpareParts.FindAsync(id);

            if (part == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng"));

            return Ok(ApiResponse.SuccessResponse(part));
        }

        [HttpPost]
        [HttpPost("add")]
        public async Task<IActionResult> AddSparePart([FromBody] SparePart part)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu phụ tùng không hợp lệ", ModelState));

            var name = (part.Name ?? string.Empty).Trim();
            var code = (part.Code ?? string.Empty).Trim();
            var location = (part.Location ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(ApiResponse.Failure("Tên phụ tùng không được để trống"));

            /*
             * Chống lỗi frontend gửi nhầm POST khi đang sửa:
             * Nếu đã có phụ tùng trùng Mã SP hoặc trùng Tên, cập nhật bản ghi đó thay vì báo lỗi.
             */
            SparePart? existing = null;

            if (!string.IsNullOrWhiteSpace(code))
            {
                existing = await _context.SpareParts
                    .FirstOrDefaultAsync(p => p.Code != null && p.Code.ToLower() == code.ToLower());
            }

            existing ??= await _context.SpareParts
                .FirstOrDefaultAsync(p => p.Name != null && p.Name.ToLower() == name.ToLower());

            if (existing != null)
            {
                existing.Name = name;
                existing.Code = code;
                existing.Price = part.Price;
                existing.StockQuantity = part.StockQuantity;
                existing.Location = location;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse.SuccessResponse(existing, "Cập nhật phụ tùng thành công"));
            }

            part.Id = 0;
            part.Name = name;
            part.Code = code;
            part.Location = location;

            _context.SpareParts.Add(part);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(part, "Thêm phụ tùng thành công"));
        }

        [HttpPut("{id}")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateSparePart(int id, [FromBody] SparePart input)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã phụ tùng không hợp lệ"));

            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu phụ tùng không hợp lệ", ModelState));

            var part = await _context.SpareParts.FindAsync(id);

            if (part == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng"));

            var name = (input.Name ?? string.Empty).Trim();
            var code = (input.Code ?? string.Empty).Trim();
            var location = (input.Location ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(ApiResponse.Failure("Tên phụ tùng không được để trống"));

            // Khi sửa, chỉ chặn Mã SP nếu mã đó đang thuộc phụ tùng khác.
            if (!string.IsNullOrWhiteSpace(code))
            {
                var existedCode = await _context.SpareParts.AnyAsync(p =>
                    p.Id != id &&
                    p.Code != null &&
                    p.Code.ToLower() == code.ToLower()
                );

                if (existedCode)
                    return BadRequest(ApiResponse.Failure("Mã phụ tùng đã tồn tại"));
            }

            part.Name = name;
            part.Code = code;
            part.Price = input.Price;
            part.StockQuantity = input.StockQuantity;
            part.Location = location;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(part, "Cập nhật phụ tùng thành công"));
        }

        [HttpDelete("{id}")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteSparePart(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã phụ tùng không hợp lệ"));

            var part = await _context.SpareParts.FindAsync(id);

            if (part == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng"));

            var used = await _context.RepairParts.AnyAsync(r => r.SparePartId == id);

            if (used)
                return BadRequest(ApiResponse.Failure("Không thể xóa phụ tùng đã được sử dụng trong phiếu sửa"));

            _context.SpareParts.Remove(part);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(null, "Xóa phụ tùng thành công"));
        }
    }
}
