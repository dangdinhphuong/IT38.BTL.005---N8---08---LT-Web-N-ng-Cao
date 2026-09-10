using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers;

/// <summary>Self-service profile: FR-USER-006/007/008. Cần đăng nhập nhưng không cần role Admin.</summary>
[ApiController]
[Route("api/users/me")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetMyProfile()
    {
        var profile = await _userService.GetMyProfileAsync();
        return Ok(ApiResponse<UserDetailResponse>.Ok(profile));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var profile = await _userService.UpdateMyProfileAsync(request);
        return Ok(ApiResponse<UserDetailResponse>.Ok(profile));
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _userService.ChangePasswordAsync(request);
        return Ok(ApiResponse<object>.Ok(new { success = true }));
    }
}
