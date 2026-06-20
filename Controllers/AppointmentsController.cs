using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.DTOs;
using AutoGarageManager.Models;
using AutoGarageManager.Helpers;
using AutoGarageManager.Services;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly GarageDbContext _context;
        private readonly IEmailService _emailService;
        private const int MaxAppointmentsPerMinute = 3;

        public AppointmentsController(GarageDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.Car)
                .Include(a => a.AppointmentServices)
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
                    ServiceName = a.AppointmentServices.Any() ? string.Join(", ", a.AppointmentServices.Select(s => s.ServiceName)) : (ExtractNoteValue(a.Note, "Dịch vụ") ?? ExtractNoteValue(a.Note, "Khách đặt lịch dịch vụ") ?? "Dịch vụ sửa chữa"),
                    Services = a.AppointmentServices.Select(s => new { s.ServiceId, s.ServiceName, s.Price }).ToList(),
                    EstimatedAmount = a.AppointmentServices.Any() ? a.AppointmentServices.Sum(s => s.Price).ToString("N0") + "đ" : ExtractNoteValue(a.Note, "Tạm tính")
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
                .Include(a => a.AppointmentServices)
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

            var type = string.IsNullOrWhiteSpace(dto.Type) ? "Bảo dưỡng định kỳ / sửa chữa" : dto.Type!.Trim();
            if (type.Contains("Lái thử", StringComparison.OrdinalIgnoreCase) || type.Contains("Test Drive", StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse.Failure("Garage sửa chữa không hỗ trợ lịch lái thử xe"));

            var customer = await FindOrCreateCustomer(dto.CustomerName, dto.CustomerAccount, dto.CustomerEmail);
            var selectedServices = NormalizeSelectedServices(dto);

            if (type.Contains("Bảo dưỡng", StringComparison.OrdinalIgnoreCase) || type.Contains("sửa chữa", StringComparison.OrdinalIgnoreCase))
            {
                if (dto.CarId == null)
                    return BadRequest(ApiResponse.Failure("Vui lòng chọn xe cần bảo dưỡng/sửa chữa"));
                if (selectedServices.Count == 0)
                    return BadRequest(ApiResponse.Failure("Vui lòng chọn ít nhất một dịch vụ sửa chữa"));
            }

            var serviceNames = selectedServices.Count > 0 ? string.Join(", ", selectedServices.Select(sv => sv.ServiceName)) : "Không chọn dịch vụ";
            var totalAmount = selectedServices.Sum(sv => sv.Price);
            var note =
                $"Loại yêu cầu: {type}\n" +
                $"Dịch vụ: {serviceNames}\n" +
                $"Số lượng dịch vụ: {selectedServices.Count}\n" +
                $"Tạm tính: {(totalAmount > 0 ? totalAmount.ToString("N0") + "đ" : "Chưa có")}\n" +
                $"Mục chọn: {dto.SelectedTarget ?? serviceNames}\n" +
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

            foreach (var sv in selectedServices)
            {
                _context.AppointmentServices.Add(new AppointmentService
                {
                    AppointmentId = appointment.AppointmentId,
                    ServiceId = sv.ServiceId,
                    ServiceName = sv.ServiceName,
                    Price = sv.Price
                });
            }
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
                appointment.Status,
                Services = selectedServices.Select(sv => new { sv.ServiceId, sv.ServiceName, sv.Price }).ToList()
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
            var result = await SetAppointmentStatus(id, "Đã xác nhận", null);
            if (result is ObjectResult objectResult && objectResult.StatusCode >= 400)
                return result;

            await CreateInvoiceFromApprovedAppointment(id);
            await SendAppointmentEmail(id, "Lịch hẹn của bạn đã được xác nhận", "Garage đã xác nhận lịch hẹn của bạn. Vui lòng đến đúng thời gian đã đặt.");
            return result;
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectPaymentDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(ApiResponse.Failure("Vui lòng nhập lý do từ chối lịch hẹn"));

            var result = await SetAppointmentStatus(id, "Đã từ chối", dto.Reason);
            if (result is ObjectResult objectResult && objectResult.StatusCode >= 400)
                return result;
            await SendAppointmentEmail(id, "Lịch hẹn của bạn đã bị từ chối", $"Lý do từ chối: {dto.Reason}");
            return result;
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
            var slotEnd = slotStart.AddHours(1);

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

                var carBusy = await _context.Appointments.AnyAsync(a =>
                    a.CarId == carId.Value &&
                    a.AppointmentDate >= slotStart &&
                    a.AppointmentDate < slotEnd &&
                    a.Status != "Đã hủy" &&
                    a.Status != "Đã từ chối");
                if (carBusy)
                    return BadRequest(ApiResponse.Failure("Xe này đã có lịch trong khung giờ đã chọn"));
            }

            return null;
        }

        
        private async Task CreateInvoiceFromApprovedAppointment(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.Car)
                .Include(a => a.AppointmentServices)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                return;

            var existedInvoice = await _context.Invoices
                .Include(i => i.RepairOrder)
                .AnyAsync(i => i.RepairOrder != null && i.RepairOrder.TechnicalNote.Contains($"APPOINTMENT_ID:{appointmentId}"));

            if (existedInvoice)
                return;

            var car = appointment.Car;
            if (car == null)
            {
                var customerId = appointment.CustomerId;
                var plate = $"SYS-APP-{customerId}";
                car = await _context.Cars.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.LicensePlate == plate);
                if (car == null)
                {
                    car = new Car
                    {
                        CustomerId = customerId,
                        LicensePlate = plate,
                        Brand = "Chưa cập nhật",
                        Model = "Đặt lịch dịch vụ",
                        Year = DateTime.Now.Year
                    };
                    _context.Cars.Add(car);
                    await _context.SaveChangesAsync();
                }
            }

            var serviceName = appointment.AppointmentServices.Any()
                ? string.Join(", ", appointment.AppointmentServices.Select(s => s.ServiceName))
                : (ExtractNoteValue(appointment.Note, "Dịch vụ") ?? "Dịch vụ sửa chữa");
            var amount = appointment.AppointmentServices.Any()
                ? appointment.AppointmentServices.Sum(s => s.Price)
                : ExtractMoneyFromNote(appointment.Note, "Tạm tính");
            if (amount <= 0) amount = 0;

            var repairOrder = new RepairOrder
            {
                CarId = car.CarId,
                RepairDate = DateTime.Now,
                ReceivedDate = DateTime.Now,
                ProblemDescription = serviceName,
                VehicleCondition = appointment.Note ?? "",
                Diagnosis = "Tạo tự động khi Admin xác nhận lịch hẹn",
                TechnicalNote = $"APPOINTMENT_ID:{appointment.AppointmentId}",
                AssignedEmployee = "Admin",
                TechnicianName = "",
                Status = "Đã xác nhận lịch hẹn"
            };

            _context.RepairOrders.Add(repairOrder);
            await _context.SaveChangesAsync();

            var invoice = new Invoice
            {
                RepairOrderId = repairOrder.RepairOrderId,
                LaborAmount = amount,
                PartAmount = 0,
                DiscountAmount = 0,
                VatPercent = 0,
                VatAmount = 0,
                TotalAmount = amount,
                PaidAmount = 0,
                RemainingAmount = amount,
                Status = "Chưa thanh toán",
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            var note = $"LOCAL_ORDER_ID:APP{appointment.AppointmentId}\n" +
                       $"CUSTOMER_NAME:{appointment.Customer?.FullName ?? "Khách hàng"}\n" +
                       $"CUSTOMER_ACCOUNT:{(string.IsNullOrWhiteSpace(appointment.Customer?.PhoneNumber) ? appointment.Customer?.Email : appointment.Customer?.PhoneNumber)}\n" +
                       $"CUSTOMER_EMAIL:{appointment.Customer?.Email ?? ""}\n" +
                       $"SERVICE:{serviceName}\n" +
                       $"NOTE:Hóa đơn tự động từ lịch hẹn đã xác nhận";

            _context.Payments.Add(new Payment
            {
                InvoiceId = invoice.InvoiceId,
                Amount = 0,
                PaymentMethod = "Chưa thanh toán",
                Status = "Chưa thanh toán",
                PaymentDate = DateTime.Now,
                Note = note
            });

            await _context.SaveChangesAsync();
        }

        private static decimal ExtractMoneyFromNote(string? note, string label)
        {
            var value = ExtractNoteValue(note, label);
            if (string.IsNullOrWhiteSpace(value)) return 0;
            var digits = new string(value.Where(char.IsDigit).ToArray());
            return decimal.TryParse(digits, out var amount) ? amount : 0;
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
                Password = PasswordHasher.HashPassword(Guid.NewGuid().ToString("N"))
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }



        private async Task SendAppointmentEmail(int appointmentId, string subject, string message)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Customer)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
                var email = appointment?.Customer?.Email;
                if (string.IsNullOrWhiteSpace(email) || email.EndsWith("@khachhang.com")) return;
                var body = $"Xin chào {appointment?.Customer?.FullName ?? "Quý khách"},<br><br>{message}<br><br>Thời gian hẹn: {appointment?.AppointmentDate:dd/MM/yyyy HH:mm}<br><br>Auto Garage";
                await _emailService.SendEmailAsync(email, subject, body);
            }
            catch
            {
                // Không chặn quy trình xác nhận/từ chối nếu email lỗi.
            }
        }

        private static List<(int? ServiceId, string ServiceName, decimal Price)> NormalizeSelectedServices(CustomerAppointmentRequestDto dto)
        {
            var result = new List<(int? ServiceId, string ServiceName, decimal Price)>();

            if (dto.Services != null)
            {
                foreach (var s in dto.Services)
                {
                    var name = string.IsNullOrWhiteSpace(s.ServiceName) ? "Dịch vụ sửa chữa" : s.ServiceName.Trim();
                    if (!result.Any(x => string.Equals(x.ServiceName, name, StringComparison.OrdinalIgnoreCase)))
                        result.Add((s.ServiceId, name, s.Price ?? 0));
                }
            }

            if (result.Count == 0 && !string.IsNullOrWhiteSpace(dto.ServiceName))
                result.Add((null, dto.ServiceName.Trim(), dto.EstimatedAmount ?? 0));

            return result;
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
