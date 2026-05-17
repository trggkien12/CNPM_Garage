using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public ServicesController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetServices()
        {
            var services = await _context.Services.OrderBy(s => s.ServiceName).ToListAsync();
            return Ok(ApiResponse.SuccessResponse(services));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetService(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã dịch vụ không hợp lệ"));

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound(ApiResponse.Failure("Không tìm thấy dịch vụ"));

            return Ok(ApiResponse.SuccessResponse(service));
        }

        [HttpPost]
        public async Task<IActionResult> CreateService([FromBody] Service service)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu dịch vụ không hợp lệ", ModelState));

            service.ServiceId = 0;
            service.ServiceName = service.ServiceName.Trim();
            service.Description = service.Description?.Trim() ?? string.Empty;

            var existed = await _context.Services.AnyAsync(s => s.ServiceName == service.ServiceName);
            if (existed) return BadRequest(ApiResponse.Failure("Tên dịch vụ đã tồn tại"));

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(service, "Thêm dịch vụ thành công"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] Service updatedService)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã dịch vụ không hợp lệ"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu dịch vụ không hợp lệ", ModelState));

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound(ApiResponse.Failure("Không tìm thấy dịch vụ"));

            service.ServiceName = updatedService.ServiceName.Trim();
            service.Price = updatedService.Price;
            service.Description = updatedService.Description?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(service, "Cập nhật dịch vụ thành công"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã dịch vụ không hợp lệ"));

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound(ApiResponse.Failure("Không tìm thấy dịch vụ"));

            var used = await _context.RepairDetails.AnyAsync(d => d.ServiceId == id);
            if (used) return BadRequest(ApiResponse.Failure("Không thể xóa dịch vụ đã được dùng trong phiếu sửa"));

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(null, "Xóa dịch vụ thành công"));
        }
    }
}
