using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.DTOs;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public CustomersController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCustomers()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.FullName)
                .Select(c => new
                {
                    c.Id,
                    c.FullName,
                    c.Email,
                    c.PhoneNumber,
                    c.Address,
                    Cars = _context.Cars.Count(x => x.CustomerId == c.Id)
                }).ToListAsync();

            return Ok(ApiResponse.SuccessResponse(customers));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã khách hàng không hợp lệ"));

            var customer = await _context.Customers
                .Where(c => c.Id == id)
                .Select(c => new { c.Id, c.FullName, c.Email, c.PhoneNumber, c.Address })
                .FirstOrDefaultAsync();

            if (customer == null) return NotFound(ApiResponse.Failure("Không tìm thấy khách hàng"));
            return Ok(ApiResponse.SuccessResponse(customer));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu khách hàng không hợp lệ", ModelState));

            dto.FullName = dto.FullName.Trim();
            dto.PhoneNumber = dto.PhoneNumber.Trim();
            dto.Email = string.IsNullOrWhiteSpace(dto.Email) ? $"{dto.PhoneNumber}@khachhang.com" : dto.Email.Trim().ToLower();
            dto.Address = dto.Address?.Trim() ?? string.Empty;

            var existed = await _context.Customers.AnyAsync(c => c.Email == dto.Email || c.PhoneNumber == dto.PhoneNumber);
            if (existed) return BadRequest(ApiResponse.Failure("Email hoặc số điện thoại đã tồn tại"));

            var customer = new Customer
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Password = dto.Password
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(new { customer.Id, customer.FullName, customer.Email, customer.PhoneNumber, customer.Address }, "Thêm khách hàng thành công"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã khách hàng không hợp lệ"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu khách hàng không hợp lệ", ModelState));

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(ApiResponse.Failure("Không tìm thấy khách hàng"));

            dto.FullName = dto.FullName.Trim();
            dto.PhoneNumber = dto.PhoneNumber.Trim();
            dto.Email = dto.Email.Trim().ToLower();
            dto.Address = dto.Address?.Trim() ?? string.Empty;

            var existed = await _context.Customers.AnyAsync(c => c.Id != id && (c.Email == dto.Email || c.PhoneNumber == dto.PhoneNumber));
            if (existed) return BadRequest(ApiResponse.Failure("Email hoặc số điện thoại đã tồn tại"));

            customer.FullName = dto.FullName;
            customer.Email = dto.Email;
            customer.PhoneNumber = dto.PhoneNumber;
            customer.Address = dto.Address;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(new { customer.Id, customer.FullName, customer.Email, customer.PhoneNumber, customer.Address }, "Cập nhật khách hàng thành công"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã khách hàng không hợp lệ"));
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound(ApiResponse.Failure("Không tìm thấy khách hàng"));

            var hasCars = await _context.Cars.AnyAsync(c => c.CustomerId == id);
            if (hasCars) return BadRequest(ApiResponse.Failure("Không thể xóa khách hàng đã có xe trong hệ thống"));

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(null, "Xóa khách hàng thành công"));
        }
    }
}
