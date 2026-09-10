using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Data.Seed;
using Couppa.Api.Middleware;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- DbContext ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// ---- Session cho Guest cart (DD-03) + Cookie Authentication (DD-02, cấu hình bên dưới qua Identity) ----
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

// ---- ASP.NET Core Identity (DD-02): Cookie Authentication do Identity tự cấu hình ----
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "couppa.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = cookieSameSite;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));

// ---- Antiforgery (SEC-07): dùng chuẩn ASP.NET Core MVC, client gửi header mặc định RequestVerificationToken ----
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "couppa.csrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = cookieSameSite;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

// ---- Rate Limiting ----
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

// ---- Application services & MVC Controllers with Views ----
builder.Services.AddAppServices();
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStaticFiles();
app.UseCors("Frontend");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

// Endpoint cấp Anti-forgery token cho client (GET, không đổi trạng thái nên không cần tự validate CSRF).
// Frontend gọi 1 lần khi trang load để lấy token gửi qua header RequestVerificationToken cho các
// action JSON (AJAX) POST/PUT/PATCH/DELETE (SEC-07).
app.MapGet("/Account/AntiForgeryToken", (Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, HttpContext httpContext) =>
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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DbSeeder.SeedAsync(db, roleManager, userManager);
    }
}

app.Run();

// Cho phép WebApplicationFactory<Program> trong Integration Test (Phase 13) tham chiếu tới entry point.
public partial class Program { }
