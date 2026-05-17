using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly GarageDbContext _context;
        private const string StatusConfirmed = "Đã xác nhận";

        public StatisticsController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalCustomers = await _context.Customers.CountAsync();
            var totalCars = await _context.Cars.CountAsync();
            var totalOrders = await _context.RepairOrders.CountAsync();
            var totalAppointments = await _context.Appointments.CountAsync();
            var totalServices = await _context.Services.CountAsync();
            var totalSpareParts = await _context.SpareParts.CountAsync();
            var lowStockParts = await _context.SpareParts.CountAsync(p => p.StockQuantity < 5);

            var invoices = await _context.Invoices.Include(i => i.Payments).ToListAsync();
            var paidInvoices = invoices.Count(i => i.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount) >= i.TotalAmount);
            var pendingQrInvoices = invoices.Count(i => i.Status == "Chờ xác nhận thanh toán QR");
            var unpaidInvoices = invoices.Count - paidInvoices - pendingQrInvoices;

            var totalRevenue = invoices
                .Where(i => i.Payments.Where(p => p.Status == StatusConfirmed).Sum(p => p.Amount) >= i.TotalAmount)
                .Sum(i => i.TotalAmount);

            var totalConfirmedPaidAmount = await _context.Payments
                .Where(p => p.Status == StatusConfirmed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var mostUsedServices = await _context.RepairDetails
                .Include(d => d.Service)
                .GroupBy(d => new { d.ServiceId, d.Service.ServiceName })
                .Select(g => new { g.Key.ServiceId, g.Key.ServiceName, Quantity = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(new
            {
                TotalCustomers = totalCustomers,
                TotalCars = totalCars,
                TotalOrders = totalOrders,
                TotalAppointments = totalAppointments,
                TotalServices = totalServices,
                TotalSpareParts = totalSpareParts,
                TotalRevenue = totalRevenue,
                TotalConfirmedPaidAmount = totalConfirmedPaidAmount,
                PaidInvoices = paidInvoices,
                PendingQrInvoices = pendingQrInvoices,
                UnpaidInvoices = unpaidInvoices,
                LowStockParts = lowStockParts,
                MostUsedServices = mostUsedServices
            }, "Thống kê dashboard"));
        }
    }
}
