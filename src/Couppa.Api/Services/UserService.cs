using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;

    public UserService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ICurrentUserService currentUser,
        IAuditLogService auditLog)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _currentUser = currentUser;
        _auditLog = auditLog;
    }

    // ---------- Self-service (me/**) ----------

    public async Task<UserDetailResponse> GetMyProfileAsync()
    {
        var user = await FindOrThrowAsync(CurrentUserId);
        return await ToResponseAsync(user);
    }

    public async Task<UserDetailResponse> UpdateMyProfileAsync(UpdateProfileRequest request)
    {
        var user = await FindOrThrowAsync(CurrentUserId);

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw AppException.Unprocessable("USER_FULL_NAME_REQUIRED", "Họ tên là bắt buộc.");
        }

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw AppException.Unprocessable("USER_UPDATE_FAILED", string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return await ToResponseAsync(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = await FindOrThrowAsync(CurrentUserId);

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
            {
                throw AppException.Unauthorized("USER_OLD_PASSWORD_INVALID", "Mật khẩu cũ không đúng");
            }

            throw AppException.Unprocessable("USER_PASSWORD_POLICY", string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // [Assumption] SRS không đặc tả có hủy session cũ sau khi đổi mật khẩu hay không (Phase 9 plan).
        // Giữ nguyên Cookie Authentication hiện tại — nằm ngoài phạm vi UserService.
    }

    // ---------- Admin ----------

    public async Task<PagedResult<UserDetailResponse>> GetAdminListAsync(
        string? search, string? role, bool? status, int page, int pageSize)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(u => (u.Email ?? "").ToLower().Contains(keyword)
                || u.FullName.ToLower().Contains(keyword));
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.IsLocked == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var userIdsInRole = await _userManager.GetUsersInRoleAsync(role);
            var ids = userIdsInRole.Select(u => u.Id).ToList();
            query = query.Where(u => ids.Contains(u.Id));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalItems = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserDetailResponse>(users.Count);
        foreach (var user in users)
        {
            items.Add(await ToResponseAsync(user));
        }

        return new PagedResult<UserDetailResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<UserDetailResponse> GetAdminDetailAsync(string id)
    {
        var user = await FindOrThrowAsync(id);
        return await ToResponseAsync(user);
    }

    public async Task<UserDetailResponse> LockUserAsync(string targetUserId, bool isLocked)
    {
        var user = await FindOrThrowAsync(targetUserId);

        // BR-14 / FR-USER-009: Admin không được TỰ KHÓA chính tài khoản đang đăng nhập của mình.
        // So sánh targetUserId (từ URL) với ICurrentUserService.UserId (từ Claims đã xác thực) — không bao giờ
        // tin id "mình" do client tự khai trong body (SEC-10).
        // Chỉ chặn khi isLocked=true: rule chỉ nói "không được tự khóa", không nói gì về tự mở khóa.
        if (isLocked && targetUserId == CurrentUserId)
        {
            throw AppException.Conflict("CANNOT_LOCK_SELF", "Không thể tự khóa tài khoản đang đăng nhập của chính mình");
        }

        user.IsLocked = isLocked;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw AppException.Unprocessable(
                "USER_UPDATE_FAILED",
                string.Join("; ", updateResult.Errors.Select(error => error.Description)));
        }

        await _auditLog.LogAsync(
            isLocked ? AuditAction.UserLock : AuditAction.UserUnlock,
            EntityType.User,
            null,
            new { UserId = user.Id, user.IsLocked });

        return await ToResponseAsync(user);
    }

    public async Task<UserDetailResponse> ChangeRoleAsync(string targetUserId, string roleName)
    {
        var user = await FindOrThrowAsync(targetUserId);

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            throw AppException.Unprocessable("USER_ROLE_INVALID", "Role không tồn tại");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        await _userManager.AddToRoleAsync(user, roleName);

        await _auditLog.LogAsync(AuditAction.UserRoleChange, EntityType.User, null, new { UserId = user.Id, Role = roleName });

        return await ToResponseAsync(user);
    }

    // ---------- Helpers ----------

    private string CurrentUserId => _currentUser.UserId
        ?? throw AppException.Unauthorized("USER_NOT_AUTHENTICATED", "Chưa đăng nhập");

    private async Task<ApplicationUser> FindOrThrowAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            throw AppException.NotFound("USER_NOT_FOUND", "Không tìm thấy người dùng.");
        }

        return user;
    }

    private async Task<UserDetailResponse> ToResponseAsync(ApplicationUser u)
    {
        var roles = await _userManager.GetRolesAsync(u);
        return new UserDetailResponse
        {
            Id = u.Id,
            Email = u.Email!,
            FullName = u.FullName,
            Phone = u.Phone,
            Role = roles.FirstOrDefault() ?? "User",
            IsLocked = u.IsLocked,
            CreatedAt = u.CreatedAt
        };
    }
}
