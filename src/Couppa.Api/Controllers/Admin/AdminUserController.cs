using Couppa.Api.Models.Requests;
using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

/// <summary>SRS 15.5 — /Admin/User/Index, /Detail render View; Lock/ChangeRole trả JsonResult.</summary>
[Route("Admin/User/[action]")]
[Authorize(Policy = "RequireAdmin")]
public class AdminUserController : Controller
{
    private readonly IUserService _userService;

    public AdminUserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? role, bool? status, int page = 1)
    {
        var pagedResult = await _userService.GetAdminListAsync(search, role, status, page, 50);
        return View(new AdminUsersViewModel { Users = pagedResult.Items.ToList() });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id)
    {
        var user = await _userService.GetAdminDetailAsync(id);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id, [FromBody] LockUserRequest request)
    {
        var user = await _userService.LockUserAsync(id, request.IsLocked);
        return Json(new { success = true, data = user });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeUserRoleRequest request)
    {
        var user = await _userService.ChangeRoleAsync(id, request.RoleName);
        return Json(new { success = true, data = user });
    }
}
