using System.Net;
using System.Text.Json;
using Couppa.Api.Models.Responses;

namespace Couppa.Api.Middleware;

/// <summary>
/// Bắt exception toàn cục. AppException (lỗi nghiệp vụ dự đoán trước) map đúng status/code/message.
/// Mọi exception khác coi là lỗi hệ thống -> luôn trả 500 với message chung, không lộ chi tiết
/// cho client (SEC-12/SEC-13), chi tiết thật được log ở server.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "AppException handled: {Code}", ex.Code);
            await WriteResponse(context, (int)ex.StatusCode,
                ApiResponse<object>.Fail(ex.Code, ex.Message, ex.Details));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponse(context, (int)HttpStatusCode.InternalServerError,
                ApiResponse<object>.Fail("INTERNAL_ERROR", "Đã có lỗi xảy ra, vui lòng thử lại sau."));
        }
    }

    private static Task WriteResponse(HttpContext context, int statusCode, ApiResponse<object> body)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return context.Response.WriteAsync(json);
    }
}
