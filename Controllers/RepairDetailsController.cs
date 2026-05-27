using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;
using AutoGarageManager.DTOs;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepairDetailsController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public RepairDetailsController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddServiceToRepair([FromBody] AddRepairDetailDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu chi tiết sửa chữa không hợp lệ", ModelState));

            var repairExists = await _context.RepairOrders.AnyAsync(r => r.RepairOrderId == dto.RepairOrderId);
            if (!repairExists) return NotFound(ApiResponse.Failure("Không tìm thấy phiếu sửa"));

            var service = await _context.Services.FindAsync(dto.ServiceId);
            if (service == null) return NotFound(ApiResponse.Failure("Không tìm thấy dịch vụ"));

            var detail = new RepairDetail
            {
                RepairOrderId = dto.RepairOrderId,
                ServiceId = dto.ServiceId,
                Quantity = dto.Quantity,
                Price = service.Price * dto.Quantity
            };

            _context.RepairDetails.Add(detail);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(detail, "Thêm chi tiết dịch vụ thành công"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRepairDetails()
        {
            var details = await _context.RepairDetails.Include(d => d.Service).ToListAsync();
            return Ok(ApiResponse.SuccessResponse(details));
        }

        [HttpGet("repair/{repairOrderId}")]
        public async Task<IActionResult> GetRepairDetails(int repairOrderId)
        {
            if (repairOrderId <= 0) return BadRequest(ApiResponse.Failure("Mã phiếu sửa không hợp lệ"));

            var details = await _context.RepairDetails
                .Where(d => d.RepairOrderId == repairOrderId)
                .Include(d => d.Service)
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(details));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRepairDetail(int id, [FromBody] AddRepairDetailDTO dto)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã chi tiết sửa chữa không hợp lệ"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu chi tiết sửa chữa không hợp lệ", ModelState));

            var detail = await _context.RepairDetails.FindAsync(id);
            if (detail == null) return NotFound(ApiResponse.Failure("Không tìm thấy chi tiết sửa chữa"));

            var repairExists = await _context.RepairOrders.AnyAsync(r => r.RepairOrderId == dto.RepairOrderId);
            if (!repairExists) return NotFound(ApiResponse.Failure("Không tìm thấy phiếu sửa"));

            var service = await _context.Services.FindAsync(dto.ServiceId);
            if (service == null) return NotFound(ApiResponse.Failure("Không tìm thấy dịch vụ"));

            detail.RepairOrderId = dto.RepairOrderId;
            detail.ServiceId = dto.ServiceId;
            detail.Quantity = dto.Quantity;
            detail.Price = service.Price * dto.Quantity;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(detail, "Cập nhật chi tiết sửa chữa thành công"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRepairDetail(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã chi tiết sửa chữa không hợp lệ"));
            var detail = await _context.RepairDetails.FindAsync(id);
            if (detail == null) return NotFound(ApiResponse.Failure("Không tìm thấy chi tiết sửa chữa"));

            _context.RepairDetails.Remove(detail);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(null, "Xóa chi tiết sửa chữa thành công"));
        }

    }
}
