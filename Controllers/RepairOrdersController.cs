using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;
using AutoGarageManager.DTOs;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepairOrdersController : ControllerBase
    {
        private readonly GarageDbContext _context;
        private static readonly string[] ValidStatuses = { "Chờ xử lý", "Đang sửa", "Hoàn thành", "Đã hủy" };

        public RepairOrdersController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<RepairOrder>>>> GetRepairOrders()
        {
            var orders = await _context.RepairOrders
                .Include(r => r.Car)
                .OrderByDescending(r => r.RepairOrderId)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<RepairOrder>>.SuccessResponse(orders));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetRepairOrder(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse<object>.Failure("Id không hợp lệ"));

            var order = await _context.RepairOrders
                .Include(r => r.Car)
                .Include(r => r.RepairDetails).ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(r => r.RepairOrderId == id);

            if (order == null)
                return NotFound(ApiResponse<object>.Failure("Không tìm thấy phiếu sửa"));

            var parts = await _context.RepairParts
                .Where(p => p.RepairOrderId == id)
                .Include(p => p.SparePart)
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new { Order = order, RepairParts = parts }));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<RepairOrder>>> CreateRepairOrder([FromBody] CreateRepairOrderDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<RepairOrder>.Failure("Dữ liệu không hợp lệ", ModelState));

            var carExists = await _context.Cars.AnyAsync(c => c.CarId == dto.CarId);
            if (!carExists)
                return NotFound(ApiResponse<RepairOrder>.Failure("Không tìm thấy xe"));

            var status = string.IsNullOrWhiteSpace(dto.Status) ? "Chờ xử lý" : dto.Status.Trim();
            if (!ValidStatuses.Contains(status))
                return BadRequest(ApiResponse<RepairOrder>.Failure("Trạng thái phiếu sửa không hợp lệ"));

            var order = new RepairOrder
            {
                CarId = dto.CarId,
                Status = status,
                RepairDate = DateTime.Now
            };

            _context.RepairOrders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRepairOrder), new { id = order.RepairOrderId }, ApiResponse<RepairOrder>.SuccessResponse(order, "Tạo phiếu sửa thành công"));
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponse<RepairOrder>>> UpdateStatus(int id, [FromBody] UpdateRepairOrderStatusDto dto)
        {
            if (id <= 0) return BadRequest(ApiResponse<RepairOrder>.Failure("Id không hợp lệ"));
            var order = await _context.RepairOrders.FindAsync(id);
            if (order == null) return NotFound(ApiResponse<RepairOrder>.Failure("Không tìm thấy phiếu sửa"));

            var status = string.IsNullOrWhiteSpace(dto.Status) ? "Chờ xử lý" : dto.Status.Trim();
            if (!ValidStatuses.Contains(status))
                return BadRequest(ApiResponse<RepairOrder>.Failure("Trạng thái phiếu sửa không hợp lệ"));

            order.Status = status;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<RepairOrder>.SuccessResponse(order, "Cập nhật trạng thái phiếu sửa thành công"));
        }

        [HttpPut("{id}/complete")]
        public async Task<ActionResult<ApiResponse<string>>> CompleteRepairOrder(int id)
        {
            var order = await _context.RepairOrders.FindAsync(id);
            if (order == null)
                return NotFound(ApiResponse<string>.Failure("Không tìm thấy phiếu sửa"));

            if (order.Status == "Đã hủy")
                return BadRequest(ApiResponse<string>.Failure("Không thể hoàn thành phiếu sửa đã hủy"));

            order.Status = "Hoàn thành";
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse(null, "Đã cập nhật trạng thái thành Hoàn thành"));
        }

        [HttpDelete("{id}")]
public async Task<ActionResult<ApiResponse<string>>> DeleteRepairOrder(int id)
{
    if (id <= 0)
        return BadRequest(ApiResponse<string>.Failure("Id không hợp lệ"));

    var order = await _context.RepairOrders
        .FirstOrDefaultAsync(r => r.RepairOrderId == id);

    if (order == null)
        return NotFound(ApiResponse<string>.Failure("Không tìm thấy phiếu sửa"));

    var hasInvoice = await _context.Invoices.AnyAsync(i => i.RepairOrderId == id);
    if (hasInvoice)
        return BadRequest(ApiResponse<string>.Failure("Không thể xóa phiếu sửa đã có hóa đơn"));

    // Lấy danh sách phụ tùng của phiếu sửa từ bảng RepairParts
    var repairParts = await _context.RepairParts
        .Include(rp => rp.SparePart)
        .Where(rp => rp.RepairOrderId == id)
        .ToListAsync();

    // Hoàn lại tồn kho phụ tùng đã trừ
    foreach (var part in repairParts)
    {
        if (part.SparePart != null)
        {
            part.SparePart.StockQuantity += part.Quantity;
        }
    }

    // Lấy danh sách dịch vụ sửa chữa liên quan
    var repairDetails = await _context.RepairDetails
        .Where(d => d.RepairOrderId == id)
        .ToListAsync();

    // Xóa dữ liệu con trước
    _context.RepairParts.RemoveRange(repairParts);
    _context.RepairDetails.RemoveRange(repairDetails);

    // Xóa phiếu sửa
    _context.RepairOrders.Remove(order);

    await _context.SaveChangesAsync();

    return Ok(ApiResponse<string>.SuccessResponse(null, "Xóa phiếu sửa thành công"));
}
    }
}
