using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;
using AutoGarageManager.Models;
using AutoGarageManager.DTOs;
using AutoGarageManager.Helpers;
using AutoGarageManager.Services;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AutoGarageManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly GarageDbContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private static readonly ConcurrentDictionary<string, (int Count, DateTime LockUntil)> LoginFailures = new();

        public AuthController(GarageDbContext context, JwtTokenService jwtTokenService, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateCustomerDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu đăng ký không hợp lệ", ModelState));

            request.FullName = request.FullName.Trim();
            request.Email = request.Email.Trim().ToLower();
            request.PhoneNumber = request.PhoneNumber.Trim();
            request.Address = request.Address?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
                return BadRequest(ApiResponse.Failure("Vui lòng nhập email thật để nhận mã OTP"));

            var existed = await _context.Customers.AnyAsync(c => c.Email == request.Email || c.PhoneNumber == request.PhoneNumber);
            if (existed)
                return BadRequest(ApiResponse.Failure("Email hoặc số điện thoại này đã được đăng ký"));

            var newCustomer = new Customer
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Password = PasswordHasher.HashPassword(request.Password),
                IsEmailVerified = false
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            await CreateAndSendOtpAsync(request.Email, "REGISTER", "Mã OTP xác thực đăng ký Auto Garage");

            return Ok(ApiResponse.SuccessResponse(new
            {
                email = newCustomer.Email,
                requireEmailVerification = true
            }, "Đăng ký thành công. Vui lòng kiểm tra email để lấy mã OTP xác thực."));
        }

        [HttpPost("verify-register-otp")]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyEmailOtpDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu OTP không hợp lệ", ModelState));

            var email = request.Email.Trim().ToLower();
            var otp = request.Otp.Trim();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy tài khoản với email này"));

            if (customer.IsEmailVerified)
                return Ok(ApiResponse.SuccessResponse(null, "Email đã được xác thực trước đó. Bạn có thể đăng nhập."));

            var otpCode = await FindValidOtpAsync(email, otp, "REGISTER");
            if (otpCode == null)
                return BadRequest(ApiResponse.Failure("OTP không đúng, đã dùng hoặc đã hết hạn"));

            otpCode.IsUsed = true;
            customer.IsEmailVerified = true;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(null, "Xác thực email thành công. Vui lòng quay lại trang đăng nhập."));
        }

        [HttpPost("resend-register-otp")]
        public async Task<IActionResult> ResendRegisterOtp([FromBody] ForgotPasswordDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Email không hợp lệ", ModelState));

            var email = request.Email.Trim().ToLower();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
                return NotFound(ApiResponse.Failure("Không tìm thấy tài khoản với email này"));

            if (customer.IsEmailVerified)
                return BadRequest(ApiResponse.Failure("Email đã được xác thực. Bạn có thể đăng nhập."));

            await CreateAndSendOtpAsync(email, "REGISTER", "Mã OTP xác thực đăng ký Auto Garage");
            return Ok(ApiResponse.SuccessResponse(null, "Đã gửi lại OTP về email."));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Email không hợp lệ", ModelState));

            var email = request.Email.Trim().ToLower();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
                return NotFound(ApiResponse.Failure("Email chưa được đăng ký trong hệ thống"));

            await CreateAndSendOtpAsync(email, "FORGOT_PASSWORD", "Mã OTP đặt lại mật khẩu Auto Garage");
            return Ok(ApiResponse.SuccessResponse(null, "Đã gửi OTP đặt lại mật khẩu về email."));
        }

        [HttpPost("reset-password-with-otp")]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Failure("Dữ liệu đặt lại mật khẩu không hợp lệ", ModelState));

            var email = request.Email.Trim().ToLower();
            var otp = request.Otp.Trim();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
                return NotFound(ApiResponse.Failure("Email chưa được đăng ký trong hệ thống"));

            var otpCode = await FindValidOtpAsync(email, otp, "FORGOT_PASSWORD");
            if (otpCode == null)
                return BadRequest(ApiResponse.Failure("OTP không đúng, đã dùng hoặc đã hết hạn"));

            customer.Password = PasswordHasher.HashPassword(request.NewPassword);
            customer.IsEmailVerified = true;
            otpCode.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(null, "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."));
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
                return BadRequest(ApiResponse.Failure("Vui lòng nhập đầy đủ tài khoản và mật khẩu", ModelState));

            var rawUsername = request.Username ?? string.Empty;
            var username = rawUsername.Trim();
            var password = request.Password ?? string.Empty;

            if (rawUsername != username)
                return BadRequest(ApiResponse.Failure("Email/số điện thoại/tài khoản không được có khoảng trắng ở đầu hoặc cuối"));

            if (username.Contains(' '))
                return BadRequest(ApiResponse.Failure("Email/số điện thoại/tài khoản không được có khoảng trắng ở giữa"));

            if (username != "admin" && !username.Contains('@') && !Regex.IsMatch(username, @"^0\d{9,10}$"))
                return BadRequest(ApiResponse.Failure("Email hoặc số điện thoại sai định dạng"));

            var lockKey = username.ToLower();
            if (LoginFailures.TryGetValue(lockKey, out var state) && state.LockUntil > DateTime.UtcNow)
                return StatusCode(429, ApiResponse.Failure("Bạn nhập sai quá nhiều lần. Vui lòng thử lại sau 1 phút"));

            var adminUsername = _configuration["AdminAccount:Username"] ?? "admin";
            var adminPassword = _configuration["AdminAccount:Password"] ?? "123456";
            if (username.Equals(adminUsername, StringComparison.OrdinalIgnoreCase) && password == adminPassword)
            {
                LoginFailures.TryRemove(lockKey, out _);
                var token = _jwtTokenService.GenerateToken(0, "admin", "Quản trị viên", "admin");
                return Ok(ApiResponse.SuccessResponse(new
                {
                    token,
                    user = new { id = 0, name = "Quản trị viên", role = "admin", user = "admin", email = "admin" },
                    rememberMe = request.RememberMe
                }, "Đăng nhập Admin thành công"));
            }

            var lowerUsername = username.ToLower();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lowerUsername || c.PhoneNumber == username);

            if (customer != null && PasswordHasher.VerifyPassword(password, customer.Password))
            {
                if (!PasswordHasher.IsHashedPassword(customer.Password))
                {
                    customer.Password = PasswordHasher.HashPassword(password);
                    await _context.SaveChangesAsync();
                }

                if (!customer.IsEmailVerified)
                {
                    return BadRequest(ApiResponse.Failure("Email chưa xác thực. Vui lòng nhập OTP đã gửi về email.", new
                    {
                        requireEmailVerification = true,
                        email = customer.Email
                    }));
                }

                LoginFailures.TryRemove(lockKey, out _);
                var token = _jwtTokenService.GenerateToken(customer.Id, customer.Email, customer.FullName, "customer");
                return Ok(ApiResponse.SuccessResponse(new
                {
                    token,
                    user = new
                    {
                        id = customer.Id,
                        name = customer.FullName,
                        fullName = customer.FullName,
                        role = "customer",
                        user = customer.Email,
                        email = customer.Email,
                        phone = customer.PhoneNumber,
                        phoneNumber = customer.PhoneNumber,
                        address = customer.Address
                    },
                    rememberMe = request.RememberMe
                }, "Đăng nhập Khách hàng thành công"));
            }

            var nextCount = LoginFailures.TryGetValue(lockKey, out var oldState) ? oldState.Count + 1 : 1;
            var lockUntil = nextCount >= 5 ? DateTime.UtcNow.AddMinutes(1) : DateTime.MinValue;
            LoginFailures[lockKey] = (nextCount, lockUntil);

            return BadRequest(ApiResponse.Failure("Sai tài khoản hoặc mật khẩu", new { failedCount = nextCount }));
        }

        private async Task CreateAndSendOtpAsync(string email, string purpose, string subject)
        {
            var normalizedEmail = email.Trim().ToLower();
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var oldOtps = await _context.EmailOtps
                .Where(x => x.Email == normalizedEmail && x.Purpose == purpose && !x.IsUsed)
                .ToListAsync();
            foreach (var old in oldOtps)
                old.IsUsed = true;

            var otp = new EmailOtp
            {
                Email = normalizedEmail,
                Code = code,
                Purpose = purpose,
                ExpiresAt = DateTime.Now.AddMinutes(5),
                IsUsed = false
            };
            _context.EmailOtps.Add(otp);
            await _context.SaveChangesAsync();

            var html = $@"
                <div style='font-family:Arial,sans-serif;max-width:520px;margin:auto;border:1px solid #e5e7eb;border-radius:14px;padding:24px'>
                    <h2 style='color:#2563eb;margin-top:0'>Auto Garage</h2>
                    <p>Xin chào,</p>
                    <p>Mã OTP của bạn là:</p>
                    <div style='font-size:32px;font-weight:800;letter-spacing:6px;background:#f1f5f9;border-radius:12px;padding:16px;text-align:center;color:#111827'>{code}</div>
                    <p style='margin-top:18px'>Mã có hiệu lực trong <b>5 phút</b>. Vui lòng không chia sẻ mã này cho người khác.</p>
                </div>";

            await _emailService.SendEmailAsync(normalizedEmail, subject, html);
        }

        private async Task<EmailOtp?> FindValidOtpAsync(string email, string otp, string purpose)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _context.EmailOtps
                .Where(x => x.Email == normalizedEmail
                    && x.Code == otp
                    && x.Purpose == purpose
                    && !x.IsUsed
                    && x.ExpiresAt > DateTime.Now)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
