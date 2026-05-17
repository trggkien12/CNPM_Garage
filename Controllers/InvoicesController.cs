using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;
using AutoGarageManager.Services;

namespace AutoGarageManager.Controllers
{
    [ApiController]
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

            decimal total = await _repairOrderService.CalculateTotalCost(repairOrderId);
            if (total <= 0)
                return BadRequest(ApiResponse.Failure("Không thể tạo hóa đơn vì phiếu sửa chưa có dịch vụ hoặc phụ tùng"));

            var invoice = new Invoice
            {
                RepairOrderId = repairOrderId,
                TotalAmount = total,
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
                .Include(i => i.Payments)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.InvoiceId,
                    i.RepairOrderId,
                    i.TotalAmount,
                    i.CreatedAt,
                    PaidAmount = i.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount),
                    PendingAmount = i.Payments.Where(p => p.Status == "Chờ xác nhận").Sum(p => p.Amount),
                    RemainingAmount = i.TotalAmount - i.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount),
                    Status = i.Status
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
    }
}
