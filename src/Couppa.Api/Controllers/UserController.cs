using Couppa.Api.Models.Requests;
using Couppa.Api.Models.ViewModels;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

[Authorize]
public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var profile = await _userService.GetMyProfileAsync();
        var viewModel = new UserProfileViewModel
        {
            User = profile,
            FullName = profile.FullName,
            Phone = profile.Phone
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        try
        {
            await _userService.UpdateMyProfileAsync(new UpdateProfileRequest
            {
                FullName = model.FullName,
                Phone = model.Phone
            });

            if (!string.IsNullOrEmpty(model.OldPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                await _userService.ChangePasswordAsync(new ChangePasswordRequest
                {
                    OldPassword = model.OldPassword,
                    NewPassword = model.NewPassword
                });
            }

            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var currentProfile = await _userService.GetMyProfileAsync();
            model.User = currentProfile;
            return View(model);
        }
    }
}
