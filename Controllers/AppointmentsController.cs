using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.DTOs;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public AppointmentsController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var appointments = await _context.Appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(appointments));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAppointment(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã lịch hẹn không hợp lệ"));

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy lịch hẹn"));

            return Ok(ApiResponse.SuccessResponse(appointment));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu lịch hẹn không hợp lệ", ModelState));

            if (dto.AppointmentDate <= DateTime.Now)
                return BadRequest(ApiResponse.Failure("Thời gian hẹn phải lớn hơn thời gian hiện tại"));

            var customerExists = await _context.Customers.AnyAsync(c => c.Id == dto.CustomerId);
            if (!customerExists)
                return NotFound(ApiResponse.Failure("Không tìm thấy khách hàng"));

            if (dto.CarId.HasValue)
            {
                var carExists = await _context.Cars.AnyAsync(c => c.CarId == dto.CarId.Value && c.CustomerId == dto.CustomerId);
                if (!carExists)
                    return BadRequest(ApiResponse.Failure("Xe không tồn tại hoặc không thuộc khách hàng này"));
            }

            var appointment = new Appointment
            {
                CustomerId = dto.CustomerId,
                CarId = dto.CarId,
                AppointmentDate = dto.AppointmentDate,
                Note = dto.Note?.Trim(),
                Status = "Chờ xác nhận",
                RejectionReason = null,
                RejectedAt = null,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(appointment, "Đặt lịch hẹn thành công"));
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Trạng thái không hợp lệ", ModelState));

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy lịch hẹn"));

            if (dto.Status == "Đã từ chối" && string.IsNullOrWhiteSpace(dto.RejectionReason))
                return BadRequest(ApiResponse.Failure("Vui lòng nhập lý do từ chối lịch hẹn"));

            appointment.Status = dto.Status;
            if (dto.Status == "Đã từ chối")
            {
                appointment.RejectionReason = dto.RejectionReason?.Trim();
                appointment.RejectedAt = DateTime.Now;
            }
            else
            {
                appointment.RejectionReason = null;
                appointment.RejectedAt = null;
            }
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(appointment, "Cập nhật lịch hẹn thành công"));
        }

        [HttpPost("{id}/convert-to-repair-order")]
        public async Task<IActionResult> ConvertToRepairOrder(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy lịch hẹn"));

            if (!appointment.CarId.HasValue)
                return BadRequest(ApiResponse.Failure("Lịch hẹn chưa có xe nên không thể chuyển thành phiếu sửa"));

            var order = new RepairOrder
            {
                CarId = appointment.CarId.Value,
                RepairDate = DateTime.Now,
                Status = "Chờ xử lý"
            };

            appointment.Status = "Đã chuyển phiếu sửa";
            _context.RepairOrders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(new { appointment, order }, "Đã chuyển lịch hẹn thành phiếu sửa"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy lịch hẹn"));

            appointment.Status = "Đã hủy";
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(appointment, "Đã hủy lịch hẹn"));
        }
    }
}
