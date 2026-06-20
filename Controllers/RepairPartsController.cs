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
    public class RepairPartsController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public RepairPartsController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRepairParts()
        {
            var data = await _context.RepairParts
                .Include(r => r.SparePart)
                .Include(r => r.RepairOrder)
                .OrderByDescending(r => r.Id)
                .ToListAsync();
            return Ok(ApiResponse.SuccessResponse(data));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRepairPart(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã phụ tùng sửa chữa không hợp lệ"));
            var repairPart = await _context.RepairParts.Include(r => r.SparePart).Include(r => r.RepairOrder).FirstOrDefaultAsync(r => r.Id == id);
            if (repairPart == null) return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng trong phiếu sửa"));
            return Ok(ApiResponse.SuccessResponse(repairPart));
        }

        [HttpGet("repair/{repairOrderId}")]
        public async Task<IActionResult> GetRepairPartsByRepairOrder(int repairOrderId)
        {
            if (repairOrderId <= 0) return BadRequest(ApiResponse.Failure("Mã phiếu sửa không hợp lệ"));
            var data = await _context.RepairParts
                .Where(r => r.RepairOrderId == repairOrderId)
                .Include(r => r.SparePart)
                .OrderByDescending(r => r.Id)
                .ToListAsync();
            return Ok(ApiResponse.SuccessResponse(data));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRepairPart([FromBody] RepairPart repairPart)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu phụ tùng sửa chữa không hợp lệ", ModelState));

            var orderExists = await _context.RepairOrders.AnyAsync(r => r.RepairOrderId == repairPart.RepairOrderId);
            if (!orderExists) return NotFound(ApiResponse.Failure("Không tìm thấy phiếu sửa"));

            var sparePart = await _context.SpareParts.FindAsync(repairPart.SparePartId);
            if (sparePart == null) return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng"));
            if (repairPart.Quantity <= 0) return BadRequest(ApiResponse.Failure("Số lượng phải lớn hơn 0"));
            if (sparePart.StockQuantity < repairPart.Quantity) return BadRequest(ApiResponse.Failure("Không đủ số lượng phụ tùng trong kho"));

            repairPart.Id = 0;
            repairPart.UnitPrice = sparePart.Price;
            repairPart.TotalPrice = repairPart.Quantity * repairPart.UnitPrice;
            sparePart.StockQuantity -= repairPart.Quantity;

            _context.RepairParts.Add(repairPart);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(repairPart, "Thêm phụ tùng vào phiếu sửa thành công"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRepairPart(int id, [FromBody] RepairPart input)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã phụ tùng sửa chữa không hợp lệ"));
            if (input.Quantity <= 0) return BadRequest(ApiResponse.Failure("Số lượng phải lớn hơn 0"));

            var repairPart = await _context.RepairParts.FindAsync(id);
            if (repairPart == null) return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng trong phiếu sửa"));

            var oldSparePart = await _context.SpareParts.FindAsync(repairPart.SparePartId);
            var newSparePart = await _context.SpareParts.FindAsync(input.SparePartId);
            if (newSparePart == null) return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng"));

            if (oldSparePart != null)
                oldSparePart.StockQuantity += repairPart.Quantity;

            if (newSparePart.StockQuantity < input.Quantity)
                return BadRequest(ApiResponse.Failure("Không đủ số lượng phụ tùng trong kho"));

            newSparePart.StockQuantity -= input.Quantity;

            repairPart.RepairOrderId = input.RepairOrderId;
            repairPart.SparePartId = input.SparePartId;
            repairPart.Quantity = input.Quantity;
            repairPart.UnitPrice = newSparePart.Price;
            repairPart.TotalPrice = input.Quantity * repairPart.UnitPrice;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(repairPart, "Cập nhật phụ tùng sửa chữa thành công"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRepairPart(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã phụ tùng sửa chữa không hợp lệ"));
            var repairPart = await _context.RepairParts.FindAsync(id);
            if (repairPart == null) return NotFound(ApiResponse.Failure("Không tìm thấy phụ tùng trong phiếu sửa"));

            var sparePart = await _context.SpareParts.FindAsync(repairPart.SparePartId);
            if (sparePart != null) sparePart.StockQuantity += repairPart.Quantity;

            _context.RepairParts.Remove(repairPart);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(null, "Xóa phụ tùng khỏi phiếu sửa thành công"));
        }
    }
}
