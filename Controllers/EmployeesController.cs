using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public EmployeesController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _context.Employees.OrderBy(e => e.Name).ToListAsync();
            return Ok(ApiResponse.SuccessResponse(employees));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã nhân viên không hợp lệ"));
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound(ApiResponse.Failure("Không tìm thấy nhân viên"));
            return Ok(ApiResponse.SuccessResponse(employee));
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] Employee employee)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu nhân viên không hợp lệ", ModelState));

            employee.Id = 0;
            employee.Name = employee.Name.Trim();
            employee.Phone = employee.Phone?.Trim() ?? string.Empty;
            employee.Position = employee.Position?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(employee.Phone))
            {
                var existedPhone = await _context.Employees.AnyAsync(e => e.Phone == employee.Phone);
                if (existedPhone) return BadRequest(ApiResponse.Failure("Số điện thoại nhân viên đã tồn tại"));
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(employee, "Thêm nhân viên thành công"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee input)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã nhân viên không hợp lệ"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse.Failure("Dữ liệu nhân viên không hợp lệ", ModelState));

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound(ApiResponse.Failure("Không tìm thấy nhân viên"));

            input.Name = input.Name.Trim();
            input.Phone = input.Phone?.Trim() ?? string.Empty;
            input.Position = input.Position?.Trim() ?? string.Empty;

            var existedPhone = await _context.Employees.AnyAsync(e => e.Id != id && e.Phone == input.Phone && input.Phone != "");
            if (existedPhone) return BadRequest(ApiResponse.Failure("Số điện thoại nhân viên đã tồn tại"));

            employee.Name = input.Name;
            employee.Phone = input.Phone;
            employee.Position = input.Position;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(employee, "Cập nhật nhân viên thành công"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse.Failure("Mã nhân viên không hợp lệ"));
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound(ApiResponse.Failure("Không tìm thấy nhân viên"));

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.SuccessResponse(null, "Xóa nhân viên thành công"));
        }
    }
}
