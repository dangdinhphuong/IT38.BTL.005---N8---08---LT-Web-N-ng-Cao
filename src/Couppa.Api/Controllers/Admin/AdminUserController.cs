using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Couppa.Api.Controllers.Admin;

/// <summary>Admin quản lý người dùng: FR-USER-001..005/009.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "RequireAdmin")]
public class AdminUserController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminUserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDetailResponse>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] short? role,
        [FromQuery] bool? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetAdminListAsync(search, role, status, page, pageSize);
        return Ok(ApiResponse<PagedResult<UserDetailResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetById(long id)
    {
        var user = await _userService.GetAdminDetailAsync(id);
        return Ok(ApiResponse<UserDetailResponse>.Ok(user));
    }

    [HttpPatch("{id:long}/lock")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> Lock(long id, [FromBody] LockUserRequest request)
    {
        var user = await _userService.LockUserAsync(id, request.IsLocked);
        return Ok(ApiResponse<UserDetailResponse>.Ok(user));
    }

    [HttpPatch("{id:long}/role")]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> ChangeRole(long id, [FromBody] ChangeUserRoleRequest request)
    {
        var user = await _userService.ChangeRoleAsync(id, request.RoleId);
        return Ok(ApiResponse<UserDetailResponse>.Ok(user));
    }
}
