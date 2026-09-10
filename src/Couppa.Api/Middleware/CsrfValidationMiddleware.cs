using Microsoft.AspNetCore.Antiforgery;
using Couppa.Api.Models.Responses;

namespace Couppa.Api.Middleware;

/// <summary>
/// SEC-07 (CSRF Protection) cho API thuần (SPA gọi qua Fetch, không dùng Razor form nên
/// [ValidateAntiForgeryToken] không áp dụng được trực tiếp — attribute đó đọc form field ẩn,
/// còn ở đây client gửi token qua header JS).
///
/// Cách làm: <see cref="IAntiforgery.ValidateRequestAsync"/> tự so khớp giá trị token trong
/// cookie "couppa.csrf" với giá trị submit — ASP.NET Core Antiforgery đã đọc submit token từ
/// đúng <c>AntiforgeryOptions.HeaderName</c> ("X-CSRF-TOKEN", cấu hình ở Program.cs) nên
/// Middleware này chỉ cần gọi API đó, không tự parse cookie/header thủ công.
///
/// Áp dụng cho mọi request POST/PUT/PATCH/DELETE tới "/api/**", TRỪ:
///   - GET /api/auth/csrf-token: endpoint CẤP token, gọi trước khi có cookie CSRF nên không thể
///     tự validate chính nó (GET cũng không đổi trạng thái nên vốn không cần CSRF).
///   - POST /api/auth/register, POST /api/auth/login: trước khi đăng nhập lần đầu, trình duyệt
///     CÓ THỂ chưa từng gọi csrf-token nên chưa chắc có cookie "couppa.csrf". Rủi ro CSRF ở 2
///     action này thấp hơn các action khác (register không có phiên đăng nhập nào bị lợi dụng;
///     login CSRF nhiều nhất chỉ khiến nạn nhân đăng nhập vào tài khoản của kẻ tấn công — không
///     tự chiếm được tài khoản nạn nhân) nên được loại trừ để đơn giản hoá luồng khởi tạo, đúng
///     theo yêu cầu review đã xác nhận. Mọi action đổi trạng thái còn lại (đổi mật khẩu, giỏ
///     hàng, CRUD, logout, admin...) đều đã có cookie session/csrf từ trước nên KHÔNG loại trừ.
/// </summary>
public class CsrfValidationMiddleware
{
    private static readonly HashSet<string> StateChangingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    // So khớp không phân biệt hoa/thường vì path routing của ASP.NET Core không phân biệt hoa/thường.
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/csrf-token",
        "/api/auth/register",
        "/api/auth/login"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfValidationMiddleware> _logger;

    public CsrfValidationMiddleware(RequestDelegate next, ILogger<CsrfValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var path = context.Request.Path;

        var isApi = path.StartsWithSegments("/api");
        var isStateChanging = StateChangingMethods.Contains(context.Request.Method);
        var isExcluded = ExcludedPaths.Contains(path.Value ?? string.Empty);

        if (isApi && isStateChanging && !isExcluded)
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException ex)
            {
                _logger.LogWarning(ex, "CSRF validation thất bại cho {Method} {Path} từ IP {IP}",
                    context.Request.Method, path, context.Connection.RemoteIpAddress);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail("CSRF_TOKEN_INVALID", "Yêu cầu không hợp lệ, vui lòng tải lại trang và thử lại."));
                return;
            }
        }

        await _next(context);
    }
}
