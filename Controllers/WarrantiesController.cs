using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            var now = DateTime.Now.Date;
            var data = await _context.Warranties.OrderByDescending(w => w.Id).ToListAsync();
            foreach (var item in data)
                item.Status = item.ExpiryDate.Date >= now ? "Còn hạn" : "Hết hạn";
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(data));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWarranty(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã bảo hành không hợp lệ"));
            var item = await _context.Warranties.FindAsync(id);
            if (item == null) return NotFound(ApiResponse.Failure("Không tìm thấy bảo hành"));
            item.Status = item.ExpiryDate.Date >= DateTime.Now.Date ? "Còn hạn" : "Hết hạn";
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(item));
        }

        [HttpPost]
        [HttpPost("add")]
        public async Task<IActionResult> CreateWarranty([FromBody] Warranty input)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu bảo hành không hợp lệ", ModelState));

            input.ServiceName = (input.ServiceName ?? string.Empty).Trim();
            input.CustomerName = (input.CustomerName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input.ServiceName)) return BadRequest(ApiResponse.Failure("Tên dịch vụ bảo hành không được để trống"));
            if (input.ExpiryDate <= input.PurchaseDate) return BadRequest(ApiResponse.Failure("Ngày hết hạn bảo hành phải lớn hơn ngày bắt đầu"));

            input.Id = 0;
            input.Status = input.ExpiryDate.Date >= DateTime.Now.Date ? "Còn hạn" : "Hết hạn";
            _context.Warranties.Add(input);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(input, "Thêm bảo hành thành công"));
        }

        [HttpPut("{id}")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateWarranty(int id, [FromBody] Warranty input)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã bảo hành không hợp lệ"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu bảo hành không hợp lệ", ModelState));

            var item = await _context.Warranties.FindAsync(id);
            if (item == null) return NotFound(ApiResponse.Failure("Không tìm thấy bảo hành"));

            input.ServiceName = (input.ServiceName ?? string.Empty).Trim();
            input.CustomerName = (input.CustomerName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input.ServiceName)) return BadRequest(ApiResponse.Failure("Tên dịch vụ bảo hành không được để trống"));
            if (input.ExpiryDate <= input.PurchaseDate) return BadRequest(ApiResponse.Failure("Ngày hết hạn bảo hành phải lớn hơn ngày bắt đầu"));

            item.CustomerId = input.CustomerId;
            item.CarId = input.CarId;
            item.ServiceId = input.ServiceId;
            item.SparePartId = input.SparePartId;
            item.InvoiceId = input.InvoiceId;
            item.ServiceName = input.ServiceName;
            item.CustomerName = input.CustomerName;
            item.PurchaseDate = input.PurchaseDate;
            item.ExpiryDate = input.ExpiryDate;
            item.Status = input.ExpiryDate.Date >= DateTime.Now.Date ? "Còn hạn" : "Hết hạn";
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(item, "Cập nhật bảo hành thành công"));
        }

        [HttpDelete("{id}")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteWarranty(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã bảo hành không hợp lệ"));
            var item = await _context.Warranties.FindAsync(id);
            if (item == null) return NotFound(ApiResponse.Failure("Không tìm thấy bảo hành"));
            _context.Warranties.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(null, "Xóa bảo hành thành công"));
        }
    }
}
