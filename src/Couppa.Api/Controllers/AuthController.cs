using System.Security.Claims;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Couppa.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ICartService cartService,
        ICurrentUserService currentUserService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _cartService = cartService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>UC-03 — FR-AUTH-001.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Register([FromBody] RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<UserResponse>.Ok(user));
    }

    /// <summary>
    /// UC-04 — FR-AUTH-002. SEC-11: rate limit 5 lần/phút/IP (policy "auth-login", cấu hình ở Program.cs).
    /// SEC-04: Session ID được regenerate sau khi đăng nhập thành công (chống Session Fixation).
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.ValidateCredentialsAsync(request);

        // FR-CART-007 / BR-13: lấy session_id Guest TRƯỚC khi Session.Clear() - sau Clear session_id cũ sẽ mất.
        var guestSessionId = _currentUserService.GetOrCreateGuestSessionId();

        // SEC-04: hủy session Guest hiện tại trước khi tạo Cookie Auth mới -> chống Session Fixation.
        HttpContext.Session.Clear();

        // Merge giỏ hàng Session (Guest) vào giỏ hàng User theo BR-13 (xem CartService.MergeGuestCartAsync
        // cho đầy đủ 4 trường hợp a/b/c/d). Không dùng kết quả merged cart ở đây để giữ nguyên UserResponse
        // hiện có (không mở rộng phạm vi response Login) - client tự gọi GET /api/cart nếu cần đọc giỏ hàng.
        await _cartService.MergeGuestCartAsync(guestSessionId, user.Id);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, user.Id.ToString()),
            new(AppClaimTypes.Role, user.Role.Name)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        var response = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.Name
        };
        return Ok(ApiResponse<UserResponse>.Ok(response));
    }

    /// <summary>UC-04 — FR-AUTH-003.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var userId = User.FindFirst(AppClaimTypes.UserId)?.Value;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        _logger.LogInformation("Đăng xuất thành công cho user {UserId}", userId);
        return Ok(ApiResponse<object>.Ok(new { success = true }));
    }
}
