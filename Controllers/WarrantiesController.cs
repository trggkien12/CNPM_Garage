using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarrantiesController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public WarrantiesController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet("list")]
        public async Task<IActionResult> GetWarranties()
        {
            var data = await _context.Warranties
                .OrderByDescending(w => w.Id)
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(data));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWarranty(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã bảo hành không hợp lệ"));

            var item = await _context.Warranties.FindAsync(id);

            if (item == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy bảo hành"));

            return Ok(ApiResponse.SuccessResponse(item));
        }

        [HttpPost]
        [HttpPost("add")]
        public async Task<IActionResult> CreateWarranty([FromBody] Warranty input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu bảo hành không hợp lệ", ModelState));

            var serviceName = (input.ServiceName ?? string.Empty).Trim();
            var customerName = (input.CustomerName ?? string.Empty).Trim();
            var purchaseDate = (input.PurchaseDate ?? string.Empty).Trim();
            var expiryDate = (input.ExpiryDate ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(serviceName))
                return BadRequest(ApiResponse.Failure("Tên dịch vụ bảo hành không được để trống"));

            /*
             * Nếu frontend gửi POST khi sửa và bảo hành đã có,
             * cập nhật bản ghi đó thay vì báo trùng.
             */
            var existing = await _context.Warranties.FirstOrDefaultAsync(w =>
                w.ServiceName != null &&
                w.CustomerName != null &&
                w.ServiceName.ToLower() == serviceName.ToLower() &&
                w.CustomerName.ToLower() == customerName.ToLower() &&
                w.PurchaseDate == purchaseDate
            );

            if (existing != null)
            {
                existing.ServiceName = serviceName;
                existing.CustomerName = customerName;
                existing.PurchaseDate = purchaseDate;
                existing.ExpiryDate = expiryDate;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse.SuccessResponse(existing, "Cập nhật bảo hành thành công"));
            }

            input.Id = 0;
            input.ServiceName = serviceName;
            input.CustomerName = customerName;
            input.PurchaseDate = purchaseDate;
            input.ExpiryDate = expiryDate;

            _context.Warranties.Add(input);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(input, "Thêm bảo hành thành công"));
        }

        [HttpPut("{id}")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateWarranty(int id, [FromBody] Warranty input)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã bảo hành không hợp lệ"));

            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu bảo hành không hợp lệ", ModelState));

            var item = await _context.Warranties.FindAsync(id);

            if (item == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy bảo hành"));

            var serviceName = (input.ServiceName ?? string.Empty).Trim();
            var customerName = (input.CustomerName ?? string.Empty).Trim();
            var purchaseDate = (input.PurchaseDate ?? string.Empty).Trim();
            var expiryDate = (input.ExpiryDate ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(serviceName))
                return BadRequest(ApiResponse.Failure("Tên dịch vụ bảo hành không được để trống"));

            var existed = await _context.Warranties.AnyAsync(w =>
                w.Id != id &&
                w.ServiceName != null &&
                w.CustomerName != null &&
                w.ServiceName.ToLower() == serviceName.ToLower() &&
                w.CustomerName.ToLower() == customerName.ToLower() &&
                w.PurchaseDate == purchaseDate
            );

            if (existed)
                return BadRequest(ApiResponse.Failure("Bảo hành đã tồn tại"));

            item.ServiceName = serviceName;
            item.CustomerName = customerName;
            item.PurchaseDate = purchaseDate;
            item.ExpiryDate = expiryDate;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(item, "Cập nhật bảo hành thành công"));
        }

        [HttpDelete("{id}")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteWarranty(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã bảo hành không hợp lệ"));

            var item = await _context.Warranties.FindAsync(id);

            if (item == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy bảo hành"));

            _context.Warranties.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(null, "Xóa bảo hành thành công"));
        }
    }
}
