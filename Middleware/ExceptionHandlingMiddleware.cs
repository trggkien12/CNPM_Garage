using System.Net;
using AutoGarageManager.Models;

namespace AutoGarageManager.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi xử lý request {Method} {Path}", context.Request.Method, context.Request.Path);

                context.Response.Clear();
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                object error = _environment.IsDevelopment()
                    ? new { Message = ex.Message, StackTrace = ex.StackTrace }
                    : new { Message = "Internal Server Error", StackTrace = (string?)null };

                var response = ApiResponse.Failure("Lỗi hệ thống, vui lòng thử lại sau", error);
                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
