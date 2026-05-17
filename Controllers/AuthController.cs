using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;
using AutoGarageManager.DTOs;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly GarageDbContext _context;
        private static readonly ConcurrentDictionary<string, (int Count, DateTime LockUntil)> LoginFailures = new();

        public AuthController(GarageDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateCustomerDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu đăng ký không hợp lệ", ModelState));

            request.FullName = request.FullName.Trim();
            request.Email = string.IsNullOrWhiteSpace(request.Email) ? $"{request.PhoneNumber}@khachhang.com" : request.Email.Trim().ToLower();
            request.PhoneNumber = request.PhoneNumber.Trim();
            request.Address = request.Address?.Trim() ?? string.Empty;

            var existed = await _context.Customers.AnyAsync(c => c.Email == request.Email || c.PhoneNumber == request.PhoneNumber);
            if (existed)
                return BadRequest(ApiResponse.Failure("Email hoặc số điện thoại này đã được đăng ký"));

            var newCustomer = new Customer
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Password = request.Password
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(new
            {
                newCustomer.Id,
                newCustomer.FullName,
                newCustomer.Email,
                newCustomer.PhoneNumber,
                newCustomer.Address
            }, "Đăng ký thành công"));
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(ApiResponse.SuccessResponse(null, "Đăng xuất thành công"));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Vui lòng nhập đầy đủ tài khoản và mật khẩu", errors = ModelState });

            var rawUsername = request.Username ?? string.Empty;
            var username = rawUsername.Trim();
            var password = request.Password ?? string.Empty;

            if (rawUsername != username)
                return BadRequest(new { message = "Số điện thoại/tài khoản không được có khoảng trắng ở đầu hoặc cuối" });

            if (username.Contains(' '))
                return BadRequest(new { message = "Số điện thoại/tài khoản không được có khoảng trắng ở giữa" });

            if (username != "admin" && !username.Contains('@') && !Regex.IsMatch(username, @"^0\d{9,10}$"))
                return BadRequest(new { message = "Số điện thoại sai định dạng" });

            var lockKey = username.ToLower();
            if (LoginFailures.TryGetValue(lockKey, out var state) && state.LockUntil > DateTime.UtcNow)
                return StatusCode(429, new { message = "Bạn nhập sai quá nhiều lần. Vui lòng thử lại sau 1 phút" });

            if (username == "admin" && password == "123456")
            {
                LoginFailures.TryRemove(lockKey, out _);
                return Ok(new
                {
                    message = "Đăng nhập Admin thành công",
                    user = new { name = "Quản trị viên", role = "admin", user = "admin" },
                    rememberMe = request.RememberMe
                });
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => (c.Email == username || c.PhoneNumber == username) && c.Password == password);

            if (customer != null)
            {
                LoginFailures.TryRemove(lockKey, out _);
                return Ok(new
                {
                    message = "Đăng nhập Khách hàng thành công",
                    user = new { id = customer.Id, name = customer.FullName, role = "customer", user = customer.Email, phone = customer.PhoneNumber },
                    rememberMe = request.RememberMe
                });
            }

            var nextCount = 1;
            if (LoginFailures.TryGetValue(lockKey, out var oldState))
                nextCount = oldState.Count + 1;

            var lockUntil = nextCount >= 5 ? DateTime.UtcNow.AddMinutes(1) : DateTime.MinValue;
            LoginFailures[lockKey] = (nextCount, lockUntil);

            return BadRequest(new { message = "Sai tài khoản hoặc mật khẩu", failedCount = nextCount });
        }
    }
}
