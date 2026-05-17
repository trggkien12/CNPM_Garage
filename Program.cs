using AutoGarageManager.Data;
using AutoGarageManager.Middleware;
using AutoGarageManager.Services; // Gọi thêm thư mục Services
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình CORS: Cho phép giao diện HTML truy cập API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. ĐĂNG KÝ SERVICE: Bắt buộc phải có dòng này để InvoicesController dùng được hàm tính tiền
builder.Services.AddScoped<RepairOrderService>();

// 3. Các dịch vụ cơ bản
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Kết nối Database
builder.Services.AddDbContext<GarageDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

var app = builder.Build();

// 5. Khởi tạo Database nếu chưa có
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GarageDbContext>();
    db.Database.EnsureCreated();
}

// 6. Cấu hình Pipeline (Middleware)
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
// Cho phép load file tĩnh (HTML) và áp dụng CORS
app.UseStaticFiles();
app.UseCors(); 

app.UseAuthorization();

app.MapControllers();

app.Run();