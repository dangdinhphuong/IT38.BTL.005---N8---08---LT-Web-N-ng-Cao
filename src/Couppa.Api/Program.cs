using Couppa.Api.Data;
using Couppa.Api.Data.Seed;
using Couppa.Api.Middleware;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- DbContext ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- CORS: bắt buộc AllowCredentials + origin cụ thể, KHÔNG dùng AllowAnyOrigin
// (trình duyệt từ chối wildcard origin kết hợp credentials -> cookie sẽ không được gửi). ----
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5500";
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ---- Session + Cookie Authentication (DD-01) ----
// SameSite=None yêu cầu Secure=true (trình duyệt từ chối None+không Secure). Khi chạy sau HTTPS
// (kể cả HTTPS chấm dứt tại reverse proxy) giữ None+Always; khi chạy HTTP thuần (vd. container
// nội bộ chưa có TLS) hạ xuống Lax+SameAsRequest để cookie vẫn được set được qua HTTP.
var useSecureCookies = builder.Configuration.GetValue("Cookies:Secure", true);
var cookieSameSite = useSecureCookies ? SameSiteMode.None : SameSiteMode.Lax;
var cookieSecurePolicy = useSecureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "couppa.session";
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.SameSite = cookieSameSite;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "couppa.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = cookieSameSite;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        // API thuần trả JSON, không redirect HTML khi 401/403 (ghi đè hành vi mặc định).
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireClaim(AppClaimTypes.Role, "Admin"));

// ---- Antiforgery (SEC-07): API thuần (JS gửi header), không dùng Razor form,
// nên validate qua CsrfValidationMiddleware (header X-CSRF-TOKEN so khớp cookie) thay vì
// [ValidateAntiForgeryToken] (chỉ dành cho MVC form truyền thống). Cookie đọc được bằng JS
// (HttpOnly=false) vì đây là giá trị JS PHẢI đọc lại để đính vào header - không phải bí mật
// (bảo mật CSRF nằm ở việc So Khớp header=cookie, kẻ tấn công cross-site không đọc được cookie
// origin khác nên không tự tạo được header khớp). ----
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "couppa.csrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = cookieSameSite;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

// ---- Rate Limiting (SEC-11): giới hạn POST /api/auth/login 5 lần/phút/IP chống brute-force ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-login", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---- Application services ----
builder.Services.AddAppServices();

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("Frontend");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// SEC-07: đặt SAU Authentication/Authorization (đã biết user là ai) nhưng TRƯỚC MapControllers
// (chặn trước khi vào action) - so khớp header X-CSRF-TOKEN với cookie couppa.csrf cho mọi
// request thay đổi trạng thái tới /api/**.
app.UseMiddleware<CsrfValidationMiddleware>();

app.MapControllers();

// Endpoint cấp CSRF token cho client (GET, không đổi trạng thái nên không cần tự validate CSRF).
// Frontend gọi 1 lần khi trang load để đảm bảo cookie couppa.csrf luôn tồn tại trước khi user
// thao tác bất kỳ hành động POST/PUT/PATCH/DELETE nào.
app.MapGet("/api/auth/csrf-token", (IAntiforgery antiforgery, HttpContext httpContext) =>
{
    var tokens = antiforgery.GetAndStoreTokens(httpContext);
    return Results.Ok(Couppa.Api.Models.Responses.ApiResponse<object>.Ok(new { token = tokens.RequestToken }));
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        await DbSeeder.SeedAsync(db);
    }
}

app.Run();

// Cho phép WebApplicationFactory<Program> trong Integration Test (Phase 13) tham chiếu tới entry point.
public partial class Program { }
