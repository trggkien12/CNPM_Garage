using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.DTOs;
using AutoGarageManager.Models;
using AutoGarageManager.Services;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly GarageDbContext _context;
        private readonly RepairOrderService _repairOrderService;
        private const string StatusConfirmed = "Đã xác nhận";

        public InvoicesController(GarageDbContext context, RepairOrderService repairOrderService)
        {
            _context = context;
            _repairOrderService = repairOrderService;
        }

        [HttpPost("{repairOrderId}")]
        public async Task<IActionResult> CreateInvoice(int repairOrderId)
        {
            if (repairOrderId <= 0)
                return BadRequest(ApiResponse.Failure("Mã phiếu sửa không hợp lệ"));

            var repairOrder = await _context.RepairOrders.FirstOrDefaultAsync(r => r.RepairOrderId == repairOrderId);
            if (repairOrder == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy phiếu sửa"));

            var existedInvoice = await _context.Invoices.FirstOrDefaultAsync(i => i.RepairOrderId == repairOrderId);
            if (existedInvoice != null)
                return BadRequest(ApiResponse.Failure("Phiếu sửa này đã có hóa đơn", existedInvoice));

            decimal laborAmount = await _repairOrderService.CalculateServiceCost(repairOrderId);
            decimal partAmount = await _repairOrderService.CalculatePartCost(repairOrderId);
            decimal discountAmount = 0;
            decimal vatPercent = 0;
            decimal subTotal = laborAmount + partAmount - discountAmount;
            decimal vatAmount = Math.Round(subTotal * vatPercent / 100m, 0);
            decimal total = subTotal + vatAmount;
            if (total <= 0)
                return BadRequest(ApiResponse.Failure("Không thể tạo hóa đơn vì phiếu sửa chưa có dịch vụ hoặc phụ tùng"));

            var invoice = new Invoice
            {
                RepairOrderId = repairOrderId,
                LaborAmount = laborAmount,
                PartAmount = partAmount,
                DiscountAmount = discountAmount,
                VatPercent = vatPercent,
                VatAmount = vatAmount,
                TotalAmount = total,
                PaidAmount = 0,
                RemainingAmount = total,
                Status = "Chưa thanh toán",
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);
            repairOrder.Status = "Hoàn thành";
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(invoice, "Tạo hóa đơn thành công"));
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoices()
        {
            var invoices = await _context.Invoices
                .Include(i => i.RepairOrder)
                    .ThenInclude(r => r.Car)
                    .ThenInclude(c => c.Customer)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.InvoiceId,
                    Id = i.InvoiceId,
                    i.RepairOrderId,
                    i.LaborAmount,
                    i.PartAmount,
                    i.DiscountAmount,
                    i.VatPercent,
                    i.VatAmount,
                    i.TotalAmount,
                    StoredPaidAmount = i.PaidAmount,
                    StoredRemainingAmount = i.RemainingAmount,
                    Amount = i.TotalAmount,
                    Price = i.TotalAmount,
                    i.CreatedAt,
                    F4 = i.CreatedAt,
                    PaidAmount = i.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount),
                    PendingAmount = i.Payments.Where(p => p.Status == "Chờ xác nhận").Sum(p => p.Amount),
                    RemainingAmount = i.TotalAmount - i.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount),
                    Status = i.Status,
                    InvoiceStatus = i.Status,
                    CustomerName = i.RepairOrder != null && i.RepairOrder.Car != null && i.RepairOrder.Car.Customer != null ? i.RepairOrder.Car.Customer.FullName : ExtractNoteValue(i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Note).FirstOrDefault(), "CUSTOMER_NAME") ?? "Khách hàng",
                    CustomerAccount = i.RepairOrder != null && i.RepairOrder.Car != null && i.RepairOrder.Car.Customer != null ? (string.IsNullOrWhiteSpace(i.RepairOrder.Car.Customer.PhoneNumber) ? i.RepairOrder.Car.Customer.Email : i.RepairOrder.Car.Customer.PhoneNumber) : ExtractNoteValue(i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Note).FirstOrDefault(), "CUSTOMER_ACCOUNT") ?? "",
                    ServiceName = ExtractNoteValue(i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Note).FirstOrDefault(), "SERVICE") 
                        ?? (i.RepairOrder != null && !string.IsNullOrWhiteSpace(i.RepairOrder.ProblemDescription) ? i.RepairOrder.ProblemDescription : "Hóa đơn dịch vụ"),
                    LocalOrderId = ExtractNoteValue(i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Note).FirstOrDefault(), "LOCAL_ORDER_ID"),
                    LatestPaymentId = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => (int?)p.PaymentId).FirstOrDefault(),
                    LatestPaymentStatus = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Status).FirstOrDefault(),
                    LatestPaymentMethod = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.PaymentMethod).FirstOrDefault(),
                    RejectReason = ExtractNoteValue(i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Note).FirstOrDefault(), "REJECT_REASON"),
                    Payments = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => new
                    {
                        p.PaymentId,
                        p.Amount,
                        p.PaymentMethod,
                        p.Status,
                        p.PaymentDate,
                        p.ConfirmedAt,
                        p.ConfirmedBy,
                        p.Note,
                        LocalOrderId = ExtractNoteValue(p.Note, "LOCAL_ORDER_ID"),
                        CustomerName = ExtractNoteValue(p.Note, "CUSTOMER_NAME"),
                        CustomerAccount = ExtractNoteValue(p.Note, "CUSTOMER_ACCOUNT"),
                        ServiceName = ExtractNoteValue(p.Note, "SERVICE"),
                        RejectReason = ExtractNoteValue(p.Note, "REJECT_REASON")
                    })
                })
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(invoices));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyInvoices()
        {
            var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(customerIdClaim, out var customerId))
                return Unauthorized(ApiResponse.Failure("Phiên đăng nhập không hợp lệ"));

            var invoices = await _context.Invoices
                .Include(i => i.RepairOrder)
                    .ThenInclude(r => r.Car)
                    .ThenInclude(c => c.Customer)
                .Include(i => i.Payments)
                .Where(i => i.RepairOrder != null && i.RepairOrder.Car != null && i.RepairOrder.Car.CustomerId == customerId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.InvoiceId,
                    Id = i.InvoiceId,
                    i.RepairOrderId,
                    i.TotalAmount,
                    Amount = i.TotalAmount,
                    i.CreatedAt,
                    PaidAmount = i.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount),
                    PendingAmount = i.Payments.Where(p => p.Status == "Chờ xác nhận").Sum(p => p.Amount),
                    RemainingAmount = i.TotalAmount - i.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount),
                    Status = i.Status,
                    CustomerName = i.RepairOrder.Car.Customer != null ? i.RepairOrder.Car.Customer.FullName : "Khách hàng",
                    CustomerAccount = i.RepairOrder.Car.Customer != null ? (string.IsNullOrWhiteSpace(i.RepairOrder.Car.Customer.PhoneNumber) ? i.RepairOrder.Car.Customer.Email : i.RepairOrder.Car.Customer.PhoneNumber) : "",
                    CustomerEmail = i.RepairOrder.Car.Customer != null ? i.RepairOrder.Car.Customer.Email : "",
                    ServiceName = ExtractNoteValue(i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Note).FirstOrDefault(), "SERVICE")
                        ?? (i.RepairOrder != null && !string.IsNullOrWhiteSpace(i.RepairOrder.ProblemDescription) ? i.RepairOrder.ProblemDescription : "Hóa đơn dịch vụ"),
                    LatestPaymentId = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => (int?)p.PaymentId).FirstOrDefault(),
                    LatestPaymentStatus = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Status).FirstOrDefault(),
                    LatestPaymentMethod = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.PaymentMethod).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(invoices));
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã hóa đơn không hợp lệ"));

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy hóa đơn"));

            var paidAmount = invoice.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount);
            var pendingAmount = invoice.Payments.Where(p => p.Status == "Chờ xác nhận").Sum(p => p.Amount);
            return Ok(ApiResponse.SuccessResponse(new
            {
                invoice.InvoiceId,
                invoice.RepairOrderId,
                invoice.TotalAmount,
                invoice.CreatedAt,
                PaidAmount = paidAmount,
                PendingAmount = pendingAmount,
                RemainingAmount = invoice.TotalAmount - paidAmount,
                Status = invoice.Status,
                Payments = invoice.Payments.OrderByDescending(p => p.PaymentDate).Select(p => new
                {
                    p.PaymentId,
                    p.Amount,
                    p.PaymentMethod,
                    p.Status,
                    p.PaymentDate,
                    p.ConfirmedAt,
                    p.ConfirmedBy,
                    p.Note
                })
            }));
        }

        [HttpPut("{id}/confirm-payment")]
        public async Task<IActionResult> ConfirmLatestPayment(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy hóa đơn"));

            var payment = invoice.Payments
                .Where(p => p.Status == "Chờ xác nhận")
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefault();

            if (payment == null)
                return BadRequest(ApiResponse.Failure("Hóa đơn không có thanh toán QR chờ xác nhận"));

            payment.Status = StatusConfirmed;
            payment.ConfirmedAt = DateTime.Now;
            payment.ConfirmedBy = "Admin";

            var paidAmount = invoice.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount);
            invoice.PaidAmount = paidAmount;
            invoice.RemainingAmount = Math.Max(0, invoice.TotalAmount - paidAmount);
            invoice.Status = paidAmount >= invoice.TotalAmount ? "Đã thanh toán" : paidAmount > 0 ? "Thanh toán một phần" : "Chưa thanh toán";

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(new { invoice, payment }, "Đã xác nhận thanh toán hóa đơn"));
        }

        [HttpPut("{id}/reject-payment")]
        public async Task<IActionResult> RejectLatestPayment(int id, [FromBody] RejectPaymentDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(ApiResponse.Failure("Vui lòng nhập lý do từ chối thanh toán"));

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy hóa đơn"));

            var payment = invoice.Payments
                .Where(p => p.Status == "Chờ xác nhận")
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefault();

            if (payment == null)
                return BadRequest(ApiResponse.Failure("Hóa đơn không có thanh toán QR chờ xác nhận"));

            payment.Status = "Đã từ chối";
            payment.ConfirmedAt = DateTime.Now;
            payment.ConfirmedBy = string.IsNullOrWhiteSpace(dto.ConfirmedBy) ? "Admin" : dto.ConfirmedBy.Trim();
            payment.Note = (payment.Note ?? "") + $"\nREJECT_REASON:{dto.Reason.Trim()}";
            invoice.Status = "Thanh toán bị từ chối";

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(new { invoice, payment }, "Đã từ chối thanh toán hóa đơn"));
        }

        [HttpGet("{id}/print")]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.RepairOrder)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy hóa đơn"));

            var paidAmount = invoice.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount);
            if (paidAmount < invoice.TotalAmount)
                return BadRequest(ApiResponse.Failure("Chỉ được in hóa đơn sau khi đã xác nhận thanh toán đủ"));

            var printData = new
            {
                Title = "HÓA ĐƠN DỊCH VỤ GARAGE",
                invoice.InvoiceId,
                invoice.RepairOrderId,
                invoice.TotalAmount,
                PaidAmount = paidAmount,
                ChangeAmount = paidAmount - invoice.TotalAmount,
                Status = "Đã thanh toán",
                PrintedAt = DateTime.Now,
                Payments = invoice.Payments.Where(p => p.Status == StatusConfirmed).OrderByDescending(p => p.PaymentDate)
            };

            return Ok(ApiResponse.SuccessResponse(printData, "Dữ liệu in hóa đơn"));
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
