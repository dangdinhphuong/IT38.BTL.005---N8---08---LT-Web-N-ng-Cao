using System.Net;

namespace Couppa.Api.Middleware;

/// <summary>
/// Base exception cho mọi lỗi nghiệp vụ có thể dự đoán trước (BR violation, not found, conflict...).
/// Middleware bắt exception này và map sang ApiResponse chuẩn hóa theo SRS Chương 19.
/// Không dùng cho lỗi hệ thống không lường trước (những lỗi đó rơi vào nhánh catch-all -> 500).
/// </summary>
public class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string Code { get; }
    public object? Details { get; }

    public AppException(HttpStatusCode statusCode, string code, string message, object? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details;
    }

    public static AppException NotFound(string code, string message) =>
        new(HttpStatusCode.NotFound, code, message);

    public static AppException Conflict(string code, string message, object? details = null) =>
        new(HttpStatusCode.Conflict, code, message, details);

    public static AppException Unprocessable(string code, string message) =>
        new(HttpStatusCode.UnprocessableEntity, code, message);

    public static AppException Unauthorized(string code, string message) =>
        new(HttpStatusCode.Unauthorized, code, message);

    public static AppException Forbidden(string code, string message) =>
        new(HttpStatusCode.Forbidden, code, message);

    public static AppException BadRequest(string code, string message) =>
        new(HttpStatusCode.BadRequest, code, message);
}
