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
                .Include(a => a.Customer)
                .Include(a => a.Car)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.AppointmentId,
                    Id = a.AppointmentId,
                    a.CustomerId,
                    a.CarId,
                    CustomerName = a.Customer != null ? a.Customer.FullName : "Khách hàng",
                    CustomerAccount = a.Customer != null ? (!string.IsNullOrWhiteSpace(a.Customer.PhoneNumber) ? a.Customer.PhoneNumber : a.Customer.Email) : "",
                    CustomerEmail = a.Customer != null ? a.Customer.Email : "",
                    CustomerPhone = a.Customer != null ? a.Customer.PhoneNumber : "",
                    CarService = a.Car != null ? (a.Car.Brand + " " + a.Car.Model + " - " + a.Car.LicensePlate) : "Dịch vụ sửa chữa",
                    LicensePlate = a.Car != null ? a.Car.LicensePlate : "",
                    a.AppointmentDate,
                    Date = a.AppointmentDate,
                    a.Note,
                    a.Status,
                    a.RejectionReason,
                    a.RejectedAt,
                    a.CreatedAt
                })
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

        [HttpPost("customer-request")]
        public async Task<IActionResult> CreateCustomerRequest([FromBody] CreateCustomerAppointmentDto dto)
        {
            if (dto == null)
                return BadRequest(ApiResponse.Failure("Dữ liệu lịch hẹn không hợp lệ"));

            if (dto.AppointmentDate <= DateTime.Now)
                return BadRequest(ApiResponse.Failure("Thời gian hẹn phải lớn hơn thời gian hiện tại"));

            var account = (dto.CustomerAccount ?? string.Empty).Trim();
            var email = (dto.CustomerEmail ?? string.Empty).Trim().ToLower();
            var phone = (dto.CustomerPhone ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(account) && !account.Contains('@'))
                phone = account;
            if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(account) && account.Contains('@'))
                email = account.ToLower();
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
                phone = account;

            Customer? customer = null;
            if (dto.CustomerId.HasValue)
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == dto.CustomerId.Value);

            if (customer == null && (!string.IsNullOrWhiteSpace(phone) || !string.IsNullOrWhiteSpace(email)))
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c =>
                    (!string.IsNullOrWhiteSpace(phone) && c.PhoneNumber == phone) ||
                    (!string.IsNullOrWhiteSpace(email) && c.Email == email));
            }

            if (customer == null)
            {
                customer = new Customer
                {
                    FullName = string.IsNullOrWhiteSpace(dto.CustomerName) ? "Khách hàng" : dto.CustomerName.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(phone) ? account : phone,
                    Email = string.IsNullOrWhiteSpace(email)
                        ? ($"{(string.IsNullOrWhiteSpace(phone) ? Guid.NewGuid().ToString("N") : phone)}@khachhang.com").ToLower()
                        : email,
                    Address = "Đăng ký/đặt lịch từ trang khách hàng",
                    Password = "123456"
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            int? carId = null;
            if (dto.CarId.HasValue)
            {
                var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == dto.CarId.Value && c.CustomerId == customer.Id);
                if (car != null) carId = car.CarId;
            }

            var serviceText = string.IsNullOrWhiteSpace(dto.CarService) ? (dto.Type ?? "Dịch vụ sửa chữa") : dto.CarService;
            var noteParts = new List<string>();
            noteParts.Add("Khách gửi yêu cầu từ trang khách hàng");
            noteParts.Add("Loại yêu cầu: " + (dto.Type ?? "Đặt lịch dịch vụ"));
            noteParts.Add("Dịch vụ/Xe: " + serviceText);
            if (!string.IsNullOrWhiteSpace(dto.SelectedTarget)) noteParts.Add("Mã chọn: " + dto.SelectedTarget);
            if (!string.IsNullOrWhiteSpace(dto.Note)) noteParts.Add("Ghi chú: " + dto.Note.Trim());
            if (dto.EstimatedAmount.HasValue && dto.EstimatedAmount.Value > 0) noteParts.Add("Tạm tính: " + dto.EstimatedAmount.Value.ToString("0") + "đ");

            var appointment = new Appointment
            {
                CustomerId = customer.Id,
                CarId = carId,
                AppointmentDate = dto.AppointmentDate,
                Note = string.Join(" | ", noteParts),
                Status = "Chờ xác nhận",
                RejectionReason = null,
                RejectedAt = null,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var result = new
            {
                appointment.AppointmentId,
                Id = appointment.AppointmentId,
                appointment.CustomerId,
                appointment.CarId,
                CustomerName = customer.FullName,
                CustomerAccount = !string.IsNullOrWhiteSpace(customer.PhoneNumber) ? customer.PhoneNumber : customer.Email,
                CustomerEmail = customer.Email,
                CustomerPhone = customer.PhoneNumber,
                CarService = serviceText,
                appointment.AppointmentDate,
                Date = appointment.AppointmentDate,
                appointment.Note,
                appointment.Status,
                appointment.RejectionReason,
                appointment.RejectedAt,
                appointment.CreatedAt,
                dto.EstimatedAmount,
                dto.SelectedTarget
            };

            return Ok(ApiResponse.SuccessResponse(result, "Đặt lịch hẹn thành công và đã gửi sang Admin"));
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
