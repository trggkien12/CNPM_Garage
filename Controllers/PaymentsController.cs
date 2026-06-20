using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.DTOs;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly GarageDbContext _context;

        private const string StatusPending = "Chờ xác nhận";
        private const string StatusConfirmed = "Đã xác nhận";
        private const string StatusCancelled = "Đã hủy";
        private const string StatusRejected = "Đã từ chối";

        private const string BankName = "VCB";
        private const string BankCode = "VCB";
        private const string BankAccountNo = "9387999288";
        private const string BankAccountName = "DO TRUNG KIEN";

        public PaymentsController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.Invoice)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    p.PaymentId,
                    Id = p.PaymentId,
                    p.InvoiceId,
                    p.Amount,
                    p.PaymentMethod,
                    p.Status,
                    p.Note,
                    p.PaymentDate,
                    p.ConfirmedAt,
                    p.ConfirmedBy,
                    InvoiceTotal = p.Invoice != null ? p.Invoice.TotalAmount : 0,
                    InvoiceStatus = p.Invoice != null ? p.Invoice.Status : "",
                    LocalOrderId = ExtractNoteValue(p.Note, "LOCAL_ORDER_ID"),
                    CustomerName = ExtractNoteValue(p.Note, "CUSTOMER_NAME"),
                    CustomerAccount = ExtractNoteValue(p.Note, "CUSTOMER_ACCOUNT"),
                    ServiceName = ExtractNoteValue(p.Note, "SERVICE")
                })
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(payments));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Id thanh toán không hợp lệ"));

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy thanh toán"));

            return Ok(ApiResponse.SuccessResponse(new
            {
                payment.PaymentId,
                payment.InvoiceId,
                payment.Amount,
                payment.PaymentMethod,
                payment.Status,
                payment.Note,
                payment.PaymentDate,
                payment.ConfirmedAt,
                payment.ConfirmedBy,
                InvoiceTotal = payment.Invoice?.TotalAmount ?? 0,
                InvoiceStatus = payment.Invoice?.Status ?? ""
            }));
        }

        [HttpGet("qr-info/{invoiceId}")]
        public async Task<IActionResult> GetQrInfo(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy hóa đơn"));

            var confirmedAmount = invoice.Payments
                .Where(p => p.Status == StatusConfirmed)
                .Sum(p => p.Amount);

            var remaining = invoice.TotalAmount - confirmedAmount;
            if (remaining <= 0)
                return BadRequest(ApiResponse.Failure("Hóa đơn này đã thanh toán đủ"));

            var content = $"THANH TOAN HD{invoice.InvoiceId}";
            var qrUrl = BuildQrUrl(remaining, content);

            return Ok(ApiResponse.SuccessResponse(new
            {
                InvoiceId = invoice.InvoiceId,
                Amount = remaining,
                BankName,
                BankCode,
                BankAccountNo,
                BankAccountName,
                TransferContent = content,
                QrUrl = qrUrl,
                Status = "Chờ khách chuyển khoản"
            }, "Thông tin QR chuyển khoản"));
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu thanh toán không hợp lệ", ModelState));

            var method = NormalizePaymentMethod(dto.PaymentMethod);
            if (method == null)
                return BadRequest(ApiResponse.Failure("Phương thức thanh toán chỉ được là Tiền mặt hoặc Chuyển khoản QR"));

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == dto.InvoiceId);

            if (invoice == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy hóa đơn"));

            var confirmedAmount = invoice.Payments
                .Where(p => p.Status == StatusConfirmed)
                .Sum(p => p.Amount);

            var remaining = invoice.TotalAmount - confirmedAmount;
            if (remaining <= 0 || invoice.Status == "Đã thanh toán")
                return BadRequest(ApiResponse.Failure("Hóa đơn này đã được thanh toán đủ, không thể thanh toán lần nữa"));

            if (dto.Amount <= 0)
                return BadRequest(ApiResponse.Failure("Số tiền thanh toán phải lớn hơn 0"));

            var pendingPaymentExists = invoice.Payments.Any(p => p.Status == StatusPending && p.PaymentMethod.Contains("QR"));
            if (method.Contains("QR") && pendingPaymentExists)
                return BadRequest(ApiResponse.Failure("Hóa đơn đang có giao dịch QR chờ xác nhận. Vui lòng kiểm tra trước khi tạo giao dịch mới"));

            var payment = new Payment
            {
                InvoiceId = dto.InvoiceId,
                Amount = dto.Amount,
                PaymentMethod = method,
                Status = method.Contains("QR") ? StatusPending : StatusConfirmed,
                PaymentDate = DateTime.Now,
                ConfirmedAt = method.Contains("QR") ? null : DateTime.Now,
                ConfirmedBy = method.Contains("QR") ? null : "Nhân viên",
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
            };

            _context.Payments.Add(payment);

            if (payment.Status == StatusConfirmed)
                await UpdateInvoiceStatus(invoice);
            else
                invoice.Status = "Chờ xác nhận thanh toán QR";

            await _context.SaveChangesAsync();

            var paidAfter = confirmedAmount + (payment.Status == StatusConfirmed ? payment.Amount : 0);
            var changeAmount = payment.Status == StatusConfirmed && dto.Amount > remaining ? dto.Amount - remaining : 0;
            var message = payment.Status == StatusPending
                ? "Đã ghi nhận khách báo đã chuyển khoản. Vui lòng Admin/Nhân viên kiểm tra ngân hàng rồi xác nhận"
                : paidAfter >= invoice.TotalAmount
                    ? (changeAmount > 0 ? $"Thanh toán thành công. Tiền thừa: {changeAmount:N0} VNĐ" : "Thanh toán hóa đơn thành công")
                    : $"Đã ghi nhận thanh toán một phần. Còn lại: {invoice.TotalAmount - paidAfter:N0} VNĐ";

            return Ok(ApiResponse.SuccessResponse(new
            {
                payment.PaymentId,
                payment.InvoiceId,
                payment.Amount,
                payment.PaymentMethod,
                payment.Status,
                payment.PaymentDate,
                InvoiceTotal = invoice.TotalAmount,
                PaidBefore = confirmedAmount,
                RequiredAmount = remaining,
                PaidAfter = paidAfter,
                ChangeAmount = changeAmount,
                InvoiceStatus = invoice.Status
            }, message));
        }

        // API cho khách bấm “Đã chuyển khoản” từ điện thoại/trang khách hàng.
        [HttpPost("qr-request")]
        public async Task<IActionResult> CreateQrRequest([FromBody] QrPaymentRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu thanh toán QR không hợp lệ", ModelState));

            var invoice = dto.InvoiceId.HasValue
                ? await _context.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == dto.InvoiceId.Value)
                : null;

            if (invoice == null)
            {
                invoice = await CreateTemporaryInvoice(dto);
            }

            var duplicatedPending = invoice.Payments.Any(p =>
                p.Status == StatusPending &&
                p.PaymentMethod.Contains("QR") &&
                (string.IsNullOrWhiteSpace(dto.LocalOrderId) || (p.Note ?? "").Contains($"LOCAL_ORDER_ID:{dto.LocalOrderId}")));

            if (duplicatedPending)
                return BadRequest(ApiResponse.Failure("Hóa đơn này đã có thanh toán QR đang chờ Admin xác nhận"));

            var note = BuildPaymentNote(dto, invoice);
            var payment = new Payment
            {
                InvoiceId = invoice.InvoiceId,
                Amount = dto.Amount,
                PaymentMethod = "Chuyển khoản QR VCB",
                Status = StatusPending,
                PaymentDate = DateTime.Now,
                ConfirmedAt = null,
                ConfirmedBy = null,
                Note = note
            };

            invoice.Status = "Chờ xác nhận thanh toán QR";
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var content = $"THANH TOAN {(string.IsNullOrWhiteSpace(dto.LocalOrderId) ? "HD" + invoice.InvoiceId : dto.LocalOrderId)}";
            return Ok(ApiResponse.SuccessResponse(new
            {
                payment.PaymentId,
                payment.InvoiceId,
                payment.Amount,
                payment.PaymentMethod,
                payment.Status,
                payment.PaymentDate,
                payment.Note,
                InvoiceStatus = invoice.Status,
                BankName,
                BankCode,
                BankAccountNo,
                BankAccountName,
                TransferContent = content,
                QrUrl = BuildQrUrl(payment.Amount, content)
            }, "Đã gửi yêu cầu xác nhận thanh toán QR. Vui lòng chờ Admin/Nhân viên kiểm tra giao dịch."));
        }

        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmPayment(int id, [FromBody] UpdatePaymentStatusDto? dto = null)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Id thanh toán không hợp lệ"));

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy thanh toán"));

            if (payment.Status == StatusConfirmed)
                return BadRequest(ApiResponse.Failure("Thanh toán này đã được xác nhận"));

            if (payment.Status == StatusCancelled || payment.Status == StatusRejected)
                return BadRequest(ApiResponse.Failure("Không thể xác nhận giao dịch đã hủy/từ chối"));

            payment.Status = StatusConfirmed;
            payment.ConfirmedAt = DateTime.Now;
            payment.ConfirmedBy = string.IsNullOrWhiteSpace(dto?.ConfirmedBy) ? "Admin" : dto!.ConfirmedBy!.Trim();
            if (!string.IsNullOrWhiteSpace(dto?.Note))
            {
                // Không ghi đè Note cũ, giữ LOCAL_ORDER_ID/CUSTOMER/SERVICE để điện thoại nhận đúng hóa đơn.
                payment.Note = (payment.Note ?? "") + $"\nADMIN_NOTE:{dto!.Note!.Trim()}";
            }

            if (payment.Invoice != null)
                await UpdateInvoiceStatus(payment.Invoice);

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(payment, "Đã xác nhận thanh toán QR thành công"));
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectPayment(int id, [FromBody] RejectPaymentDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(ApiResponse.Failure("Vui lòng nhập lý do từ chối thanh toán"));

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy thanh toán"));

            if (payment.Status == StatusConfirmed)
                return BadRequest(ApiResponse.Failure("Không thể từ chối giao dịch đã xác nhận"));

            payment.Status = StatusRejected;
            payment.ConfirmedAt = DateTime.Now;
            payment.ConfirmedBy = string.IsNullOrWhiteSpace(dto.ConfirmedBy) ? "Admin" : dto.ConfirmedBy.Trim();
            payment.Note = (payment.Note ?? "") + $"\nREJECT_REASON:{dto.Reason.Trim()}";

            if (payment.Invoice != null)
                await UpdateInvoiceStatus(payment.Invoice);

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(payment, "Đã từ chối thanh toán QR và lưu lý do"));
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelPayment(int id, [FromBody] UpdatePaymentStatusDto? dto = null)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Id thanh toán không hợp lệ"));

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy thanh toán"));

            if (payment.Status == StatusConfirmed)
                return BadRequest(ApiResponse.Failure("Không thể hủy giao dịch đã xác nhận"));

            payment.Status = StatusCancelled;
            payment.Note = string.IsNullOrWhiteSpace(dto?.Note)
                ? (payment.Note ?? "") + "\nCANCEL_REASON:Admin/Nhân viên hủy giao dịch"
                : (payment.Note ?? "") + $"\nCANCEL_REASON:{dto!.Note!.Trim()}";

            if (payment.Invoice != null)
                await UpdateInvoiceStatus(payment.Invoice);

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(payment, "Đã hủy giao dịch thanh toán"));
        }

        private async Task<Invoice> CreateTemporaryInvoice(QrPaymentRequestDto dto)
        {
            var customer = await FindOrCreateCustomer(dto.CustomerName, dto.CustomerAccount, dto.CustomerEmail);

            // Tạo xe hệ thống tạm nếu khách chưa có xe. Biển số có tiền tố SYS nên không trùng biển số thật.
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CustomerId == customer.Id);
            if (car == null)
            {
                var plate = $"SYS-{customer.Id}";
                car = await _context.Cars.FirstOrDefaultAsync(c => c.LicensePlate == plate);
                if (car == null)
                {
                    car = new Car
                    {
                        CustomerId = customer.Id,
                        LicensePlate = plate,
                        Brand = "Chưa cập nhật",
                        Model = "Thanh toán QR",
                        Year = DateTime.Now.Year
                    };
                    _context.Cars.Add(car);
                    await _context.SaveChangesAsync();
                }
            }

            var order = new RepairOrder
            {
                CarId = car.CarId,
                RepairDate = DateTime.Now,
                Status = "Hoàn thành"
            };

            _context.RepairOrders.Add(order);
            await _context.SaveChangesAsync();

            var invoice = new Invoice
            {
                RepairOrderId = order.RepairOrderId,
                LaborAmount = dto.Amount,
                PartAmount = 0,
                DiscountAmount = 0,
                VatPercent = 0,
                VatAmount = 0,
                TotalAmount = dto.Amount,
                PaidAmount = 0,
                RemainingAmount = dto.Amount,
                Status = "Chờ xác nhận thanh toán QR",
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
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
                Password = AutoGarageManager.Helpers.PasswordHasher.HashPassword(Guid.NewGuid().ToString("N"))
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        private static string BuildPaymentNote(QrPaymentRequestDto dto, Invoice invoice)
        {
            var lines = new List<string>
            {
                $"LOCAL_ORDER_ID:{dto.LocalOrderId ?? "HD" + invoice.InvoiceId}",
                $"CUSTOMER_NAME:{dto.CustomerName ?? "Khách hàng"}",
                $"CUSTOMER_ACCOUNT:{dto.CustomerAccount ?? dto.CustomerEmail ?? ""}",
                $"SERVICE:{dto.ServiceName ?? "Hóa đơn dịch vụ"}",
                $"CLIENT_NOTE:{dto.Note ?? ""}"
            };
            return string.Join("\n", lines);
        }

        private static string? NormalizePaymentMethod(string method)
        {
            var value = (method ?? string.Empty).Trim().ToLowerInvariant();
            if (value is "tiền mặt" or "tien mat" or "cash") return "Tiền mặt";
            if (value is "chuyển khoản" or "chuyen khoan" or "qr" or "qr vcb" or "vietqr") return "Chuyển khoản QR";
            return null;
        }

        private Task UpdateInvoiceStatus(Invoice invoice)
        {
            var confirmedAmount = invoice.Payments
                .Where(p => p.Status == StatusConfirmed)
                .Sum(p => p.Amount);

            var hasPendingQr = invoice.Payments.Any(p => p.Status == StatusPending);
            var hasRejectedOrCancelled = invoice.Payments.Any(p => p.Status == StatusRejected || p.Status == StatusCancelled);

            invoice.PaidAmount = confirmedAmount;
            invoice.RemainingAmount = Math.Max(0, invoice.TotalAmount - confirmedAmount);

            invoice.Status = confirmedAmount >= invoice.TotalAmount
                ? "Đã thanh toán"
                : confirmedAmount > 0 ? "Thanh toán một phần"
                : hasPendingQr ? "Chờ xác nhận thanh toán QR"
                : hasRejectedOrCancelled ? "Thanh toán bị từ chối"
                : "Chưa thanh toán";

            return Task.CompletedTask;
        }

        private static string BuildQrUrl(decimal amount, string content)
        {
            return $"https://img.vietqr.io/image/{BankCode}-{BankAccountNo}-compact2.png?amount={amount:0}&addInfo={Uri.EscapeDataString(content)}&accountName={Uri.EscapeDataString(BankAccountName)}";
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
