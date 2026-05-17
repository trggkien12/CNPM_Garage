namespace AutoGarageManager.Models
{
    /// <summary>
    /// Chuẩn hóa định dạng trả về cho mọi API. Giúp Frontend luôn nhận được một cấu trúc cố định (Success, Message, Data, Error).
    /// Phiên bản này dùng cho các API không cần trả về dữ liệu cụ thể (VD: Xóa nhân viên thành công).
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        public object? Error { get; set; }

        public ApiResponse(bool success, string? message = null, object? data = null, object? error = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Error = error;
        }

    
        /// Hàm tiện ích (Factory method) giúp tạo nhanh một kết quả Thành công ở Controller mà không cần gọi "new ApiResponse(...)" dài dòng.
   
        public static ApiResponse SuccessResponse(object? data = null, string? message = null)
            => new ApiResponse(true, message, data);

   
        /// Hàm tiện ích tạo nhanh kết quả Thất bại/Lỗi.
    
        public static ApiResponse Failure(string? message = null, object? error = null)
            => new ApiResponse(false, message, null, error);
    }

   
    /// Phiên bản Generic (<T>) của ApiResponse. 
    /// Dùng khi API trả về một kiểu dữ liệu cụ thể (VD: trả về Employee thì T là Employee). Giúp code chặt chẽ và an toàn kiểu (Type-safe) hơn.
   
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        
        // Data ở đây sẽ mang đúng kiểu T được truyền vào thay vì kiểu object chung chung
        public T? Data { get; set; }
        
        public object? Error { get; set; }

        public ApiResponse(bool success, string? message = null, T? data = default, object? error = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Error = error;
        }

        public static ApiResponse<T> SuccessResponse(T? data = default, string? message = null)
            => new ApiResponse<T>(true, message, data);

        public static ApiResponse<T> Failure(string? message = null, object? error = null)
            => new ApiResponse<T>(false, message, default, error);
    }
}