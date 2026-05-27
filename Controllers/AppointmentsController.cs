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
        private const int MaxAppointmentsPerMinute = 3;

        public AppointmentsController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.Car)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new
                {
                    a.AppointmentId,
                    Id = a.AppointmentId,
                    a.CustomerId,
                    CustomerName = a.Customer != null ? a.Customer.FullName : "Khách hàng",
                    CustomerAccount = a.Customer != null ? (string.IsNullOrWhiteSpace(a.Customer.PhoneNumber) ? a.Customer.Email : a.Customer.PhoneNumber) : "",
                    CustomerEmail = a.Customer != null ? a.Customer.Email : "",
                    CustomerPhone = a.Customer != null ? a.Customer.PhoneNumber : "",
                    a.CarId,
                    CarInfo = a.Car != null ? (a.Car.Brand + " " + a.Car.Model + " - " + a.Car.LicensePlate).Trim() : "Chưa chọn xe",
                    LicensePlate = a.Car != null ? a.Car.LicensePlate : "",
                    CarType = a.Car != null ? (a.Car.Brand + " " + a.Car.Model + " " + a.Car.Year).Trim() : "",
                    a.AppointmentDate,
                    Date = a.AppointmentDate.ToString("yyyy-MM-dd HH:mm"),
                    a.Note,
                    a.Status,
                    a.RejectionReason,
                    a.RejectedAt,
                    a.CreatedAt,
                    Type = ExtractNoteValue(a.Note, "Loại yêu cầu") ?? "Lịch hẹn dịch vụ",
                    ServiceName = ExtractNoteValue(a.Note, "Dịch vụ") ?? ExtractNoteValue(a.Note, "Khách đặt lịch dịch vụ") ?? "Dịch vụ sửa chữa",
                    EstimatedAmount = ExtractNoteValue(a.Note, "Tạm tính")
                })
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(appointments));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAppointment(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã lịch hẹn không hợp lệ"));

            var appointment = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.Car)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy lịch hẹn"));

            return Ok(ApiResponse.SuccessResponse(appointment));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu lịch hẹn không hợp lệ", ModelState));

            var validate = await ValidateAppointment(dto.CustomerId, dto.CarId, dto.AppointmentDate);
            if (validate != null) return validate;

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

        // API dành cho khách hàng đặt lịch từ khachhang.html / điện thoại qua Cloudflared.
        [HttpPost("customer-request")]
        public async Task<IActionResult> CreateCustomerRequest([FromBody] CustomerAppointmentRequestDto dto)
        {
            if (dto == null)
                return BadRequest(ApiResponse.Failure("Dữ liệu đặt lịch không hợp lệ"));

            if (!dto.AppointmentDate.HasValue)
                return BadRequest(ApiResponse.Failure("Vui lòng chọn ngày giờ hẹn"));

            var customer = await FindOrCreateCustomer(dto.CustomerName, dto.CustomerAccount, dto.CustomerEmail);
            var note =
                $"Loại yêu cầu: {(string.IsNullOrWhiteSpace(dto.Type) ? "Yêu cầu dịch vụ sửa chữa" : dto.Type!.Trim())}\n" +
                $"Dịch vụ: {(string.IsNullOrWhiteSpace(dto.ServiceName) ? "Dịch vụ sửa chữa" : dto.ServiceName!.Trim())}\n" +
                $"Tạm tính: {(dto.EstimatedAmount.HasValue ? dto.EstimatedAmount.Value.ToString("N0") + "đ" : "Chưa có")}\n" +
                $"Mục chọn: {dto.SelectedTarget ?? "Không có"}\n" +
                $"Ghi chú khách hàng: {dto.Note ?? ""}";

            var validate = await ValidateAppointment(customer.Id, dto.CarId, dto.AppointmentDate.Value);
            if (validate != null) return validate;

            var appointment = new Appointment
            {
                CustomerId = customer.Id,
                CarId = dto.CarId,
                AppointmentDate = dto.AppointmentDate.Value,
                Note = note,
                Status = "Chờ xác nhận",
                RejectionReason = null,
                RejectedAt = null,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(new
            {
                appointment.AppointmentId,
                appointment.CustomerId,
                CustomerName = customer.FullName,
                CustomerAccount = string.IsNullOrWhiteSpace(customer.PhoneNumber) ? customer.Email : customer.PhoneNumber,
                appointment.CarId,
                appointment.AppointmentDate,
                appointment.Note,
                appointment.Status
            }, "Đặt lịch hẹn thành công"));
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Trạng thái không hợp lệ", ModelState));

            return await SetAppointmentStatus(id, dto.Status, dto.RejectionReason);
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            return await SetAppointmentStatus(id, "Đã xác nhận", null);
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectPaymentDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(ApiResponse.Failure("Vui lòng nhập lý do từ chối lịch hẹn"));

            return await SetAppointmentStatus(id, "Đã từ chối", dto.Reason);
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

        private async Task<IActionResult?> ValidateAppointment(int customerId, int? carId, DateTime appointmentDate)
        {
            if (appointmentDate <= DateTime.Now)
                return BadRequest(ApiResponse.Failure("Ngày hẹn không hợp lệ, vui lòng chọn thời gian trong tương lai"));

            var slotStart = new DateTime(appointmentDate.Year, appointmentDate.Month, appointmentDate.Day, appointmentDate.Hour, appointmentDate.Minute, 0);
            var slotEnd = slotStart.AddMinutes(1);

            var countInSlot = await _context.Appointments.CountAsync(a =>
                a.AppointmentDate >= slotStart &&
                a.AppointmentDate < slotEnd &&
                a.Status != "Đã hủy" &&
                a.Status != "Đã từ chối");

            if (countInSlot >= MaxAppointmentsPerMinute)
                return BadRequest(ApiResponse.Failure("Khung giờ này đã quá nhiều lịch hẹn, vui lòng chọn giờ khác"));

            var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
            if (!customerExists)
                return NotFound(ApiResponse.Failure("Không tìm thấy khách hàng"));

            if (carId.HasValue)
            {
                var carExists = await _context.Cars.AnyAsync(c => c.CarId == carId.Value && c.CustomerId == customerId);
                if (!carExists)
                    return BadRequest(ApiResponse.Failure("Xe không tồn tại hoặc không thuộc khách hàng này"));
            }

            return null;
        }

        private async Task<Customer> FindOrCreateCustomer(string? name, string? account, string? email)
        {
            var cleanName = string.IsNullOrWhiteSpace(name) ? "Khách hàng" : name.Trim();
            var cleanAccount = (account ?? string.Empty).Trim();
            var cleanEmail = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(cleanEmail) && cleanAccount.Contains('@'))
                cleanEmail = cleanAccount.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(cleanEmail) && !string.IsNullOrWhiteSpace(cleanAccount))
                cleanEmail = $"{cleanAccount}@khachhang.com";

            var customer = await _context.Customers.FirstOrDefaultAsync(c =>
                (!string.IsNullOrWhiteSpace(cleanAccount) && (c.PhoneNumber == cleanAccount || c.Email == cleanAccount)) ||
                (!string.IsNullOrWhiteSpace(cleanEmail) && c.Email == cleanEmail));

            if (customer != null) return customer;

            customer = new Customer
            {
                FullName = cleanName,
                Email = string.IsNullOrWhiteSpace(cleanEmail) ? $"{Guid.NewGuid():N}@khachhang.com" : cleanEmail,
                PhoneNumber = cleanAccount,
                Address = "",
                Password = "AUTO_CREATED"
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        private async Task<IActionResult> SetAppointmentStatus(int id, string status, string? reason)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy lịch hẹn"));

            if (status == "Đã từ chối" && string.IsNullOrWhiteSpace(reason))
                return BadRequest(ApiResponse.Failure("Vui lòng nhập lý do từ chối lịch hẹn"));

            appointment.Status = status;

            if (status == "Đã từ chối")
            {
                appointment.RejectionReason = reason!.Trim();
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

        private static string? ExtractNoteValue(string? note, string key)
        {
            if (string.IsNullOrWhiteSpace(note)) return null;

            var lines = note.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;

                var left = line[..idx].Trim();
                var right = line[(idx + 1)..].Trim();

                if (left.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return right;
            }

            return null;
        }
    }
}
