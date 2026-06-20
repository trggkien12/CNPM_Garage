using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            var totalCustomersTask = _context.Customers.CountAsync();
            var totalCarsTask = _context.Cars.CountAsync();
            var totalOrdersTask = _context.RepairOrders.CountAsync();
            var totalAppointmentsTask = _context.Appointments.CountAsync();
            var totalServicesTask = _context.Services.CountAsync();
            var totalSparePartsTask = _context.SpareParts.CountAsync();
            var lowStockPartsTask = _context.SpareParts.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
            var pendingAppointmentsTask = _context.Appointments.CountAsync(a => a.Status.Contains("Chờ"));
            var pendingQrInvoicesTask = _context.Invoices.CountAsync(i => i.Status.Contains("Chờ xác nhận thanh toán"));
            var totalConfirmedPaidAmountTask = _context.Payments
                .Where(p => p.Status == StatusConfirmed || p.Status == "Đã thanh toán")
                .SumAsync(p => (decimal?)p.Amount);

            await Task.WhenAll(
                totalCustomersTask,
                totalCarsTask,
                totalOrdersTask,
                totalAppointmentsTask,
                totalServicesTask,
                totalSparePartsTask,
                lowStockPartsTask,
                pendingAppointmentsTask,
                pendingQrInvoicesTask,
                totalConfirmedPaidAmountTask
            );

            var invoices = await _context.Invoices.Include(i => i.Payments).ToListAsync();
            var paidInvoices = invoices.Count(i => i.Payments.Where(p => p.Status == StatusConfirmed || p.Status == "Đã thanh toán").Sum(p => p.Amount) >= i.TotalAmount);
            var unpaidInvoices = invoices.Count - paidInvoices - pendingQrInvoicesTask.Result;

            var totalRevenue = invoices
                .Where(i => i.Payments.Where(p => p.Status == StatusConfirmed || p.Status == "Đã thanh toán").Sum(p => p.Amount) >= i.TotalAmount)
                .Sum(i => i.TotalAmount);

            // Nếu có thanh toán xác nhận nhưng tổng invoice chưa khớp, vẫn dùng số tiền đã xác nhận để dashboard không bị thiếu doanh thu.
            var totalConfirmedPaidAmount = totalConfirmedPaidAmountTask.Result ?? 0;
            var dashboardRevenue = Math.Max(totalRevenue, totalConfirmedPaidAmount);

            var mostUsedServices = await _context.RepairDetails
                .Include(d => d.Service)
                .GroupBy(d => new { d.ServiceId, d.Service.ServiceName })
                .Select(g => new { g.Key.ServiceId, g.Key.ServiceName, Quantity = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(new
            {
                TotalCustomers = totalCustomersTask.Result,
                TotalCars = totalCarsTask.Result,
                TotalOrders = totalOrdersTask.Result,
                TotalAppointments = totalAppointmentsTask.Result,
                TotalPendingAppointments = pendingAppointmentsTask.Result,
                TotalServices = totalServicesTask.Result,
                TotalSpareParts = totalSparePartsTask.Result,
                TotalRevenue = dashboardRevenue,
                TotalConfirmedPaidAmount = totalConfirmedPaidAmount,
                PaidInvoices = paidInvoices,
                PendingQrInvoices = pendingQrInvoicesTask.Result,
                UnpaidInvoices = unpaidInvoices,
                LowStockParts = lowStockPartsTask.Result,
                MostUsedServices = mostUsedServices
            }, "Thống kê dashboard"));
        }
    }
}
