# HƯỚNG DẪN SỬ DỤNG WEB GARAGE TH2K

## 1. Cách chạy project

Mở terminal tại thư mục project, chạy:

```powershell
dotnet restore
dotnet run
```

Khi terminal hiện dòng kiểu:

```text
Now listening on: http://localhost:5121
```

Mở trình duyệt và vào:

```text
http://localhost:5121/login.html
```

Nếu trình duyệt vẫn hiện giao diện cũ, bấm:

```text
Ctrl + F5
```

## 2. Tài khoản Admin

Tài khoản quản trị dùng để quản lý toàn bộ hệ thống.

```text
Tài khoản: admin
Mật khẩu: 123456
```

Admin/Nhân viên có thể:

- Xem dashboard tổng quan
- Quản lý khách hàng
- Quản lý xe
- Quản lý dịch vụ sửa chữa
- Quản lý phụ tùng
- Quản lý lịch hẹn
- Xác nhận hoặc từ chối lịch hẹn
- Tạo hóa đơn
- Xác nhận thanh toán QR

## 3. Đăng ký khách hàng

Khách hàng vào trang đăng nhập và chọn **Đăng ký**.

Thông tin cần nhập:

- Họ tên
- Số điện thoại
- Mật khẩu

Sau khi đăng ký thành công:

- Tài khoản khách được lưu vào hệ thống
- Thông tin khách tự động xuất hiện trong Admin → Khách hàng
- Khách có thể đăng nhập để dùng trang khách hàng

## 4. Trang khách hàng

Sau khi đăng nhập, khách hàng có các mục chính:

```text
Trang chủ
Dịch vụ sửa chữa
Xe của tôi
Đặt lịch
Hóa đơn của tôi
Tài khoản
```

## 5. Quản lý “Xe của tôi”

Trong mục **Xe của tôi**, khách hàng có thể:

- Xem danh sách xe của mình
- Thêm xe mới
- Sửa thông tin xe
- Xóa xe
- Đặt lịch sửa chữa cho xe đó

Mỗi khách chỉ thấy xe của tài khoản mình. Khách khác không thấy xe này.

Thông tin xe gồm:

```text
Biển số xe
Hãng xe
Dòng xe
Năm sản xuất
Trạng thái
```

Ví dụ:

```text
Biển số: 14A-12345
Hãng xe: Toyota
Dòng xe: Vios
Năm sản xuất: 2021
Trạng thái: Đang hoạt động
```

## 6. Đặt lịch sửa chữa

Khách hàng có thể đặt lịch theo 2 cách:

### Cách 1: Đặt lịch từ dịch vụ

Vào **Dịch vụ sửa chữa** → chọn dịch vụ → bấm **Đặt lịch ngay**.

### Cách 2: Đặt lịch từ xe

Vào **Xe của tôi** → chọn xe → bấm **Đặt lịch cho xe này**.

Khi đặt lịch, khách cần chọn:

- Xe cần sửa
- Dịch vụ mong muốn
- Ngày giờ hẹn
- Ghi chú tình trạng xe nếu có

## 7. Admin xử lý lịch hẹn

Admin vào mục **Lịch hẹn**.

Admin có thể:

- Bấm **Xác nhận** nếu nhận lịch
- Bấm **Từ chối** nếu không nhận lịch

Nếu từ chối, hệ thống yêu cầu nhập lý do.

Ví dụ lý do:

```text
Gara đã kín lịch vào khung giờ này, vui lòng chọn ngày khác.
```

Khách hàng vào **Đặt lịch** sẽ thấy trạng thái và lý do từ chối.

## 8. Hóa đơn của tôi

Mục **Hóa đơn của tôi** chỉ hiển thị hóa đơn của đúng tài khoản khách đang đăng nhập.

Khách A không nhìn thấy hóa đơn của khách B.

Luồng đúng:

```text
Khách đặt lịch
Admin xác nhận lịch
Hệ thống tạo hóa đơn tạm tính hoặc Admin tạo hóa đơn
Khách vào Hóa đơn của tôi
Khách thanh toán QR nếu hóa đơn chưa thanh toán
```

## 9. Thanh toán QR

Khi hóa đơn chưa thanh toán, khách bấm **Thanh toán QR**.

Hệ thống hiển thị mã QR VCB với thông tin:

```text
Ngân hàng: VCB
Số tài khoản: 9387999288
Chủ tài khoản: DO TRUNG KIEN
Nội dung: THANH TOAN + mã hóa đơn
```

Sau khi khách chuyển khoản, khách bấm **Đã chuyển khoản**.

Trạng thái hóa đơn chuyển sang:

```text
Chờ Admin/Nhân viên xác nhận thanh toán QR
```

Admin kiểm tra tài khoản ngân hàng rồi bấm xác nhận. Khi đó hóa đơn mới chuyển sang **Đã hoàn tất** và được tính doanh thu.

## 10. Quản lý dịch vụ và phụ tùng

### Dịch vụ sửa chữa mẫu

```text
Thay dầu máy
Rửa xe cao cấp
Kiểm tra phanh
Bảo dưỡng định kỳ
Phủ Ceramic cao cấp
Vệ sinh nội thất
Cân bằng lốp
```

### Phụ tùng mẫu

```text
Lọc dầu
Má phanh trước
Bugi
Ắc quy GS
Lốp Michelin
Nước làm mát
Gạt mưa
```

Phụ tùng nên do Admin/Nhân viên quản lý, không nên cho khách tự mua trực tiếp.

## 11. Xóa dữ liệu cũ khi test

Nếu bạn đã chạy nhiều bản khác nhau, trình duyệt có thể lưu dữ liệu cũ trong Local Storage.

Cách xóa:

```text
F12 → Application → Local Storage → localhost → Clear
```

Sau đó tải lại trang bằng:

```text
Ctrl + F5
```

## 12. Lỗi thường gặp

### Không bấm được menu khách hàng

Nguyên nhân thường do JavaScript lỗi hoặc trình duyệt còn cache.

Cách xử lý:

```text
Ctrl + F5
```

Nếu vẫn lỗi, mở:

```text
F12 → Console
```

rồi xem dòng lỗi màu đỏ.

### Không thấy QR

QR chỉ hiện trong **Hóa đơn của tôi** khi hóa đơn chưa thanh toán. QR không hiện ở phần chọn dịch vụ vì chọn dịch vụ mới chỉ là đặt lịch, chưa phải hóa đơn chính thức.

### Khách khác vẫn thấy xe/hóa đơn cũ

Hãy xóa Local Storage rồi test lại bằng 2 tài khoản khác nhau.

## 13. Ghi chú về file khachhang.js

File JavaScript của trang khách hàng hiện được đặt đúng tại:

```text
wwwroot/js/khachhang.js
```

File này xử lý:

- Chuyển tab giao diện khách hàng
- Hiển thị thông tin khách đang đăng nhập
- Quản lý Xe của tôi
- Đặt lịch sửa chữa
- Hiển thị lý do từ chối lịch hẹn
- Hiển thị hóa đơn theo từng tài khoản
- Thanh toán QR
- Đăng xuất

Trong `khachhang.html`, file này được gọi bằng:

```html
<script src="/js/khachhang.js" defer></script>
```
