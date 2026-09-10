using Couppa.Api.Data.Entities;
using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Couppa.Api.Controllers;

/// <summary>
/// DD-02: dùng trực tiếp SignInManager/UserManager của ASP.NET Core Identity — không qua AuthService tự viết.
/// Identity tự cấu hình Cookie Authentication (ConfigureApplicationCookie ở Program.cs).
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        ICartService cartService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLog,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _cartService = cartService;
        _currentUserService = currentUserService;
        _auditLog = auditLog;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Giữ session_id Guest TRƯỚC khi đăng nhập thành công để merge cart (FR-CART-007).
        var guestSessionId = _currentUserService.GetOrCreateGuestSessionId();

        var user = await _userManager.FindByEmailAsync(model.Email);

        // FR-AUTH-005/BR-07: tài khoản IsLocked=true bị từ chối trước khi thử xác thực password.
        if (user is not null && user.IsLocked)
        {
            _logger.LogWarning("Login rejected for locked account {Email} from {IpAddress}.",
                model.Email, HttpContext.Connection.RemoteIpAddress);
            return LoginError(model, StatusCodes.Status403Forbidden, "ACCOUNT_LOCKED", "Tài khoản đã bị khóa");
        }

        // Sai email hoặc sai password đều trả cùng 1 message chung (không tiết lộ email tồn tại hay không).
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            var message = result.IsLockedOut
                ? "Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần, vui lòng thử lại sau."
                : "Email hoặc mật khẩu không đúng";
            _logger.LogWarning("Login failed for {Email} from {IpAddress}.",
                model.Email, HttpContext.Connection.RemoteIpAddress);
            return LoginError(model,
                result.IsLockedOut ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized,
                result.IsLockedOut ? "ACCOUNT_LOCKED" : "INVALID_CREDENTIALS",
                message);
        }

        // FR-CART-007 / BR-13: merge giỏ hàng Guest vào giỏ hàng User ngay sau khi đăng nhập thành công.
        await _cartService.MergeGuestCartAsync(guestSessionId, user!.Id);
        await _auditLog.LogAsync(AuditAction.LoginSuccess, EntityType.User, null,
            new { userId = user.Id, email = user.Email });
        _logger.LogInformation("User {UserId} logged in successfully.", user.Id);

        TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName}.";

        if (WantsJson())
        {
            return Ok(new
            {
                success = true,
                data = new
                {
                    user = new { id = user.Id, email = user.Email, fullName = user.FullName },
                    cart = await _cartService.GetCurrentCartAsync()
                }
            });
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Consumes("application/json")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth-login")]
    [ActionName(nameof(Login))]
    public Task<IActionResult> LoginJson([FromBody] LoginViewModel model) => Login(model);

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim().ToLowerInvariant(),
            Email = model.Email.Trim().ToLowerInvariant(),
            FullName = model.FullName.Trim(),
            Phone = model.PhoneNumber,
            IsLocked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                // BR-04: email trùng -> Identity trả lỗi code "DuplicateUserName"/"DuplicateEmail".
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        if (!await _roleManager.RoleExistsAsync("User"))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole("User"));
            if (!roleResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, error = new { code = "ROLE_SETUP_FAILED", message = "Đã có lỗi xảy ra, vui lòng thử lại sau.", details = (object?)null } });
            }
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!addRoleResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, error = new { code = "ROLE_ASSIGNMENT_FAILED", message = "Đã có lỗi xảy ra, vui lòng thử lại sau.", details = (object?)null } });
        }

        _logger.LogInformation("User {UserId} registered with email {Email}.", user.Id, user.Email);

        if (WantsJson())
        {
            return StatusCode(StatusCodes.Status201Created,
                new { success = true, data = new { id = user.Id, email = user.Email, fullName = user.FullName } });
        }

        TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Register))]
    public Task<IActionResult> RegisterJson([FromBody] RegisterViewModel model) => Register(model);

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _currentUserService.UserId;
        await _signInManager.SignOutAsync();
        HttpContext.Session.Clear();
        await _auditLog.LogAsync(AuditAction.Logout, EntityType.User, null, new { userId });
        _logger.LogInformation("User {UserId} logged out.", userId);
        TempData["SuccessMessage"] = "Đã đăng xuất thành công.";

        if (WantsJson())
        {
            return Ok(new { success = true });
        }
        return RedirectToAction("Index", "Home");
    }

    private IActionResult LoginError(LoginViewModel model, int statusCode, string code, string message)
    {
        if (WantsJson())
        {
            return StatusCode(statusCode, new
            {
                success = false,
                error = new { code, message, details = (object?)null }
            });
        }

        ModelState.AddModelError(string.Empty, message);
        return View(model);
    }

    private bool WantsJson() =>
        Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true
        || Request.Headers.Accept.Any(value =>
            value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
}
