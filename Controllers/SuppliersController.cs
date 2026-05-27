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
        [HttpGet("list")]
        public async Task<IActionResult> GetSuppliers()
        {
            var data = await _context.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(data));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplier(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã nhà cung cấp không hợp lệ"));

            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy nhà cung cấp"));

            return Ok(ApiResponse.SuccessResponse(supplier));
        }

        [HttpPost]
        [HttpPost("add")]
        public async Task<IActionResult> CreateSupplier([FromBody] Supplier supplier)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu nhà cung cấp không hợp lệ", ModelState));

            var name = (supplier.Name ?? string.Empty).Trim();
            var phone = (supplier.Phone ?? string.Empty).Trim();
            var address = (supplier.Address ?? string.Empty).Trim();
            var status = string.IsNullOrWhiteSpace(supplier.Status) ? "Hoạt động" : supplier.Status.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(ApiResponse.Failure("Tên nhà cung cấp không được để trống"));

            /*
             * Nếu frontend gửi POST khi sửa và tên NCC đã có,
             * cập nhật NCC đó thay vì báo trùng.
             */
            var existing = await _context.Suppliers.FirstOrDefaultAsync(s =>
                s.Name != null &&
                s.Name.ToLower() == name.ToLower()
            );

            if (existing != null)
            {
                existing.Name = name;
                existing.Phone = phone;
                existing.Address = address;
                existing.Status = status;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse.SuccessResponse(existing, "Cập nhật nhà cung cấp thành công"));
            }

            supplier.Id = 0;
            supplier.Name = name;
            supplier.Phone = phone;
            supplier.Address = address;
            supplier.Status = status;

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(supplier, "Thêm nhà cung cấp thành công"));
        }

        [HttpPut("{id}")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] Supplier input)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã nhà cung cấp không hợp lệ"));

            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu nhà cung cấp không hợp lệ", ModelState));

            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy nhà cung cấp"));

            var name = (input.Name ?? string.Empty).Trim();
            var phone = (input.Phone ?? string.Empty).Trim();
            var address = (input.Address ?? string.Empty).Trim();
            var status = string.IsNullOrWhiteSpace(input.Status) ? "Hoạt động" : input.Status.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(ApiResponse.Failure("Tên nhà cung cấp không được để trống"));

            var existed = await _context.Suppliers.AnyAsync(s =>
                s.Id != id &&
                s.Name != null &&
                s.Name.ToLower() == name.ToLower()
            );

            if (existed)
                return BadRequest(ApiResponse.Failure("Tên nhà cung cấp đã tồn tại"));

            supplier.Name = name;
            supplier.Phone = phone;
            supplier.Address = address;
            supplier.Status = status;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(supplier, "Cập nhật nhà cung cấp thành công"));
        }

        [HttpDelete("{id}")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã nhà cung cấp không hợp lệ"));

            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy nhà cung cấp"));

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(null, "Đã xóa nhà cung cấp thành công"));
        }
    }
}
