using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public SuppliersController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            var data = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            return Ok(ApiResponse.SuccessResponse(data));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplier(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã nhà cung cấp không hợp lệ"));
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound(ApiResponse.Failure("Không tìm thấy nhà cung cấp"));
            return Ok(ApiResponse.SuccessResponse(supplier));
        }

        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] Supplier supplier)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu nhà cung cấp không hợp lệ", ModelState));

            supplier.Id = 0;
            supplier.Name = supplier.Name.Trim();
            supplier.Phone = supplier.Phone?.Trim();
            supplier.Address = supplier.Address?.Trim();
            supplier.Status = string.IsNullOrWhiteSpace(supplier.Status) ? "Hoạt động" : supplier.Status.Trim();

            var existed = await _context.Suppliers.AnyAsync(s => s.Name == supplier.Name);
            if (existed) return BadRequest(ApiResponse.Failure("Tên nhà cung cấp đã tồn tại"));

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(supplier, "Thêm nhà cung cấp thành công"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] Supplier input)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã nhà cung cấp không hợp lệ"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu nhà cung cấp không hợp lệ", ModelState));

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound(ApiResponse.Failure("Không tìm thấy nhà cung cấp"));

            input.Name = input.Name.Trim();
            var existed = await _context.Suppliers.AnyAsync(s => s.Id != id && s.Name == input.Name);
            if (existed) return BadRequest(ApiResponse.Failure("Tên nhà cung cấp đã tồn tại"));

            supplier.Name = input.Name;
            supplier.Phone = input.Phone?.Trim();
            supplier.Address = input.Address?.Trim();
            supplier.Status = string.IsNullOrWhiteSpace(input.Status) ? "Hoạt động" : input.Status.Trim();
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(supplier, "Cập nhật nhà cung cấp thành công"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã nhà cung cấp không hợp lệ"));

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound(ApiResponse.Failure("Không tìm thấy nhà cung cấp"));

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(null, "Đã xóa nhà cung cấp thành công"));
        }
    }
}
