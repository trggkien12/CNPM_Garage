# Auto Garage Manager

Dự án quản lý garage ô tô sử dụng ASP.NET Core Web API.

## Yêu cầu hệ thống

- .NET 10.0
- SQL Server (hoặc SQL Server Express)

## Cài đặt và chạy

1. **Clone repository**:
   ```
   git clone <repository-url>
   cd AutoGarageManager
   ```

2. **Khôi phục dependencies**:
   ```
   dotnet restore
   ```

3. **Cập nhật chuỗi kết nối database**:
   - Mở `appsettings.json`
   - Thay đổi `DefaultConnection` với chuỗi kết nối SQL Server của bạn.

4. **Chạy migrations để tạo database**:
   ```
   dotnet ef database update
   Lỗi: "PendingModelChangesWarning" cách khác phục là "dotnet ef migrations add UpdateCustomerModel" (Dùng khi có model gì mới thay đổi mà chưa cập nhật vào migration) 
   ```

5. **Chạy ứng dụng**:
   ```
   dotnet run
   ```

Ứng dụng sẽ chạy tại `https://localhost:7187` hoặc `http://localhost:5121` (theo `launchSettings.json`).

## API Documentation

Swagger UI có sẵn tại `https://localhost:7187/swagger` hoặc `http://localhost:5121/swagger` khi chạy ở chế độ Development.

## Test API

Sử dụng file `AutoGarageManager.http` để test các endpoint cơ bản với REST Client extension trong VS Code.

### Ví dụ endpoint:

- **GET** `/api/cars` - Lấy danh sách xe
- **POST** `/api/cars` - Tạo xe mới
- **GET** `/api/customers` - Lấy danh sách khách hàng
- **POST** `/api/customers` - Tạo khách hàng mới
- **GET** `/api/repairorders` - Lấy danh sách phiếu sửa chữa
- **POST** `/api/repairorders` - Tạo phiếu sửa chữa mới

## Cấu trúc dự án

- **Controllers/**: API endpoints
- **Models/**: Entity models và ApiResponse
- **DTOs/**: Data Transfer Objects cho validation
- **Data/**: DbContext và migrations
- **Services/**: Business logic
- **Middleware/**: Exception handling

## Tính năng chính

- Quản lý khách hàng, xe, dịch vụ sửa chữa
- Tạo và quản lý phiếu sửa chữa
- Tính toán chi phí dịch vụ và phụ tùng
- API RESTful với validation và error handling

- **GET** `/api/customers` - Lấy danh sách khách hàng
- **POST** `/api/repairorders` - Tạo phiếu sửa chữa

## Cấu trúc dự án

- **Controllers/**: API endpoints
- **Models/**: Entity models và ApiResponse
- **DTOs/**: Data Transfer Objects cho validation
- **Data/**: DbContext và migrations
- **Services/**: Business logic
- **Middleware/**: Exception handling

## Tính năng chính

- Quản lý khách hàng, xe, dịch vụ sửa chữa
- Tạo và quản lý phiếu sửa chữa
- Tính toán chi phí dịch vụ và phụ tùng
- API RESTful với validation và error handling  

## Thành viên thực hiện

- **Đỗ Trung Kiên**: Hệ thống cốt lõi, Xác thực nhân viên & Thống kê[cite: 1].
- **Nguyễn Trung Kiên**: Quản lý Khách hàng, Xe & Thanh toán[cite: 1].
- **Đinh Mạnh Tú**: Nghiệp vụ Sửa chữa & Hóa đơn[cite: 1].
- **Trương Triệu Việt Hoàng**: Quản lý Dịch vụ, Phụ tùng & Nhà cung cấp[cite: 1].