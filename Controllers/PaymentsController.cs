using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.DTOs;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly GarageDbContext _context;

        private const string StatusPending = "Chờ xác nhận";
        private const string StatusConfirmed = "Đã xác nhận";
        private const string StatusCancelled = "Đã hủy";

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
                    p.InvoiceId,
                    p.Amount,
                    p.PaymentMethod,
                    p.Status,
                    p.Note,
                    p.PaymentDate,
                    p.ConfirmedAt,
                    p.ConfirmedBy,
                    InvoiceTotal = p.Invoice != null ? p.Invoice.TotalAmount : 0
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
                InvoiceTotal = payment.Invoice?.TotalAmount ?? 0
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
            var qrUrl = $"https://img.vietqr.io/image/{BankCode}-{BankAccountNo}-compact2.png?amount={remaining:0}&addInfo={Uri.EscapeDataString(content)}&accountName={Uri.EscapeDataString(BankAccountName)}";

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

            if (dto.Amount < remaining)
                return BadRequest(ApiResponse.Failure($"Thanh toán thiếu tiền. Còn thiếu {remaining - dto.Amount:N0} VNĐ"));

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

            var changeAmount = payment.Status == StatusConfirmed ? dto.Amount - remaining : 0;
            var message = payment.Status == StatusPending
                ? "Đã ghi nhận khách báo đã chuyển khoản. Vui lòng Admin/Nhân viên kiểm tra ngân hàng rồi xác nhận"
                : (changeAmount > 0 ? $"Thanh toán thành công. Tiền thừa: {changeAmount:N0} VNĐ" : "Thanh toán hóa đơn thành công");

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
                ChangeAmount = changeAmount,
                InvoiceStatus = invoice.Status
            }, message));
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

            if (payment.Status == StatusCancelled)
                return BadRequest(ApiResponse.Failure("Không thể xác nhận giao dịch đã hủy"));

            payment.Status = StatusConfirmed;
            payment.ConfirmedAt = DateTime.Now;
            payment.ConfirmedBy = string.IsNullOrWhiteSpace(dto?.ConfirmedBy) ? "Admin" : dto!.ConfirmedBy!.Trim();
            if (!string.IsNullOrWhiteSpace(dto?.Note)) payment.Note = dto!.Note!.Trim();

            if (payment.Invoice != null)
                await UpdateInvoiceStatus(payment.Invoice);

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(payment, "Đã xác nhận thanh toán QR thành công"));
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
            payment.Note = string.IsNullOrWhiteSpace(dto?.Note) ? "Admin/Nhân viên hủy giao dịch" : dto!.Note!.Trim();

            if (payment.Invoice != null)
                await UpdateInvoiceStatus(payment.Invoice);

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(payment, "Đã hủy giao dịch thanh toán"));
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
            invoice.Status = confirmedAmount >= invoice.TotalAmount
                ? "Đã thanh toán"
                : hasPendingQr ? "Chờ xác nhận thanh toán QR" : "Chưa thanh toán";

            return Task.CompletedTask;
        }
    }
}
