using Couppa.Api.Data.Entities;
using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

/// <summary>
/// DD-02: dùng trực tiếp SignInManager/UserManager của ASP.NET Core Identity — không qua AuthService tự viết.
/// Identity tự cấu hình Cookie Authentication (ConfigureApplicationCookie ở Program.cs).
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUserService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICartService cartService,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _cartService = cartService;
        _currentUserService = currentUserService;
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
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa");
            return View(model);
        }

        // Sai email hoặc sai password đều trả cùng 1 message chung (không tiết lộ email tồn tại hay không).
        var result = user is null
            ? SignInResult.Failed
            : await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            var message = result.IsLockedOut
                ? "Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần, vui lòng thử lại sau."
                : "Email hoặc mật khẩu không đúng";
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        // FR-CART-007 / BR-13: merge giỏ hàng Guest vào giỏ hàng User ngay sau khi đăng nhập thành công.
        await _cartService.MergeGuestCartAsync(guestSessionId, user!.Id);

        TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName}.";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

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

        await _userManager.AddToRoleAsync(user, "User");

        TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        HttpContext.Session.Clear();
        TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }
}
