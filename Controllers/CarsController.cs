using AutoGarageManager.Data;
using AutoGarageManager.DTOs;
using AutoGarageManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AutoGarageManager.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CarsController : ControllerBase
    {
        private readonly GarageDbContext _context;

        public CarsController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCars()
        {
            var cars = await _context.Cars
                .Include(c => c.Customer)
                .OrderByDescending(c => c.CarId)
                .Select(c => new
                {
                    c.CarId,
                    Id = c.CarId,
                    c.LicensePlate,
                    c.Brand,
                    c.Model,
                    c.Year,
                    c.CustomerId,
                    CustomerName = c.Customer != null ? c.Customer.FullName : "Khách hàng",
                    CustomerPhone = c.Customer != null ? c.Customer.PhoneNumber : "",
                    CustomerEmail = c.Customer != null ? c.Customer.Email : "",
                    Status = "Đang hoạt động"
                })
                .ToListAsync();

            return Ok(ApiResponse.SuccessResponse(cars));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Car>>> GetCar(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse<Car>.Failure("Id không hợp lệ"));

            var car = await _context.Cars.Include(c => c.Customer).FirstOrDefaultAsync(c => c.CarId == id);
            if (car == null) return NotFound(ApiResponse<Car>.Failure("Không tìm thấy xe"));

            return Ok(ApiResponse<Car>.SuccessResponse(car));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<Car>>> CreateCar([FromBody] CreateCarDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse<Car>.Failure("Dữ liệu không hợp lệ", ModelState));

            dto.LicensePlate = NormalizePlate(dto.LicensePlate);
            dto.Brand = dto.Brand.Trim();
            dto.Model = dto.Model.Trim();

            var customerExists = await _context.Customers.AnyAsync(c => c.Id == dto.CustomerId);
            if (!customerExists) return NotFound(ApiResponse<Car>.Failure("Không tìm thấy khách hàng"));

            var plateExists = await _context.Cars.AnyAsync(c => c.LicensePlate.ToUpper() == dto.LicensePlate);
            if (plateExists) return BadRequest(ApiResponse<Car>.Failure("Biển số xe này đã được khách hàng khác đăng ký. Không thể đăng ký trùng biển số."));

            var car = new Car
            {
                LicensePlate = dto.LicensePlate,
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year,
                CustomerId = dto.CustomerId
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCar), new { id = car.CarId }, ApiResponse<Car>.SuccessResponse(car, "Tạo xe thành công"));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<Car>>> UpdateCar(int id, [FromBody] UpdateCarDto dto)
        {
            if (id <= 0) return BadRequest(ApiResponse<Car>.Failure("Id không hợp lệ"));
            if (!ModelState.IsValid) return BadRequest(ApiResponse<Car>.Failure("Dữ liệu không hợp lệ", ModelState));

            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound(ApiResponse<Car>.Failure("Không tìm thấy xe"));

            dto.LicensePlate = NormalizePlate(dto.LicensePlate);
            dto.Brand = dto.Brand.Trim();
            dto.Model = dto.Model.Trim();

            var customerExists = await _context.Customers.AnyAsync(c => c.Id == dto.CustomerId);
            if (!customerExists) return NotFound(ApiResponse<Car>.Failure("Không tìm thấy khách hàng"));

            var plateExists = await _context.Cars.AnyAsync(c => c.CarId != id && c.LicensePlate.ToUpper() == dto.LicensePlate);
            if (plateExists) return BadRequest(ApiResponse<Car>.Failure("Biển số xe này đã được khách hàng khác đăng ký. Không thể đăng ký trùng biển số."));

            car.LicensePlate = dto.LicensePlate;
            car.Brand = dto.Brand;
            car.Model = dto.Model;
            car.Year = dto.Year;
            car.CustomerId = dto.CustomerId;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<Car>.SuccessResponse(car, "Cập nhật xe thành công"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteCar(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse<string>.Failure("Id không hợp lệ"));

            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound(ApiResponse<string>.Failure("Không tìm thấy xe"));

            var hasOrder = await _context.RepairOrders.AnyAsync(o => o.CarId == id);
            if (hasOrder) return BadRequest(ApiResponse<string>.Failure("Không thể xóa xe đã có phiếu sửa"));

            var hasAppointment = await _context.Appointments.AnyAsync(a => a.CarId == id && a.Status != "Đã hủy" && a.Status != "Đã từ chối");
            if (hasAppointment) return BadRequest(ApiResponse<string>.Failure("Không thể xóa xe đang có lịch hẹn"));

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse(null, "Xóa xe thành công"));
        }

        private static string NormalizePlate(string plate)
        {
            return (plate ?? string.Empty).Trim().ToUpper();
        }
    }
}
