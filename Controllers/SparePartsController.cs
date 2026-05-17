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
            var parts = await _context.SpareParts.OrderBy(p => p.Name).ToListAsync();
            return Ok(ApiResponse.SuccessResponse(parts));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSparePart(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã phụ tùng không hợp lệ"));
            var part = await _context.SpareParts.FindAsync(id);
            if (part == null) return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng"));
            return Ok(ApiResponse.SuccessResponse(part));
        }

        [HttpPost]
        [HttpPost("add")]
        public async Task<IActionResult> AddSparePart([FromBody] SparePart part)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu phụ tùng không hợp lệ", ModelState));

            part.Id = 0;
            part.Name = part.Name.Trim();

            var existed = await _context.SpareParts.AnyAsync(p => p.Name == part.Name);
            if (existed) return BadRequest(ApiResponse.Failure("Tên phụ tùng đã tồn tại"));

            _context.SpareParts.Add(part);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(part, "Thêm phụ tùng thành công"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSparePart(int id, [FromBody] SparePart input)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã phụ tùng không hợp lệ"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu phụ tùng không hợp lệ", ModelState));

            var part = await _context.SpareParts.FindAsync(id);
            if (part == null) return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng"));

            input.Name = input.Name.Trim();
            var existed = await _context.SpareParts.AnyAsync(p => p.Id != id && p.Name == input.Name);
            if (existed) return BadRequest(ApiResponse.Failure("Tên phụ tùng đã tồn tại"));

            part.Name = input.Name;
            part.Price = input.Price;
            part.StockQuantity = input.StockQuantity;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(part, "Cập nhật phụ tùng thành công"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSparePart(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã phụ tùng không hợp lệ"));
            var part = await _context.SpareParts.FindAsync(id);
            if (part == null) return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng"));

            var used = await _context.RepairParts.AnyAsync(r => r.SparePartId == id);
            if (used) return BadRequest(ApiResponse.Failure("Không thể xóa phụ tùng đã được sử dụng trong phiếu sửa"));

            _context.SpareParts.Remove(part);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(null, "Xóa phụ tùng thành công"));
        }
    }
}
