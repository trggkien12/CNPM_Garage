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
    public class EmployeesController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public EmployeesController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet("list")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _context.Employees
                .OrderBy(e => e.Name)
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(employees));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã nhân viên không hợp lệ"));

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy nhân viên"));

            return Ok(ApiResponse.SuccessResponse(employee));
        }

        [HttpPost]
        [HttpPost("add")]
        public async Task<IActionResult> AddEmployee([FromBody] Employee employee)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu nhân viên không hợp lệ", ModelState));

            var name = (employee.Name ?? string.Empty).Trim();
            var phone = (employee.Phone ?? string.Empty).Trim();
            var position = (employee.Position ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(ApiResponse.Failure("Tên nhân viên không được để trống"));

            /*
             * Nếu frontend gửi nhầm POST khi sửa và SĐT đã có,
             * cập nhật nhân viên đó thay vì báo trùng.
             */
            Employee? existing = null;

            if (!string.IsNullOrWhiteSpace(phone))
            {
                existing = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Phone != null && e.Phone == phone);
            }

            if (existing != null)
            {
                existing.Name = name;
                existing.Phone = phone;
                existing.Position = position;
                existing.Salary = employee.Salary;

                await _context.SaveChangesAsync();

                return Ok(ApiResponse.SuccessResponse(existing, "Cập nhật nhân viên thành công"));
            }

            employee.Id = 0;
            employee.Name = name;
            employee.Phone = phone;
            employee.Position = position;

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(employee, "Thêm nhân viên thành công"));
        }

        [HttpPut("{id}")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee input)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã nhân viên không hợp lệ"));

            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu nhân viên không hợp lệ", ModelState));

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy nhân viên"));

            var name = (input.Name ?? string.Empty).Trim();
            var phone = (input.Phone ?? string.Empty).Trim();
            var position = (input.Position ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(ApiResponse.Failure("Tên nhân viên không được để trống"));

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var existedPhone = await _context.Employees.AnyAsync(e =>
                    e.Id != id &&
                    e.Phone != null &&
                    e.Phone == phone
                );

                if (existedPhone)
                    return BadRequest(ApiResponse.Failure("Số điện thoại nhân viên đã tồn tại"));
            }

            employee.Name = name;
            employee.Phone = phone;
            employee.Position = position;
            employee.Salary = input.Salary;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(employee, "Cập nhật nhân viên thành công"));
        }

        [HttpDelete("{id}")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            if (id <= 0)
                return BadRequest(ApiResponse.Failure("Mã nhân viên không hợp lệ"));

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy nhân viên"));

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(null, "Xóa nhân viên thành công"));
        }
    }
}
