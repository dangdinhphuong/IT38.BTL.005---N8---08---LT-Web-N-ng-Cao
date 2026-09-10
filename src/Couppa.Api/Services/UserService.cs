using System.Text.RegularExpressions;
using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

public class UserService : IUserService
{
    // BR-06: tối thiểu 8 ký tự, ít nhất 1 chữ hoa, ít nhất 1 chữ số. Giữ nhất quán với AuthService.RegisterAsync.
    private static readonly Regex PasswordPolicyRegex = new(@"^(?=.*[A-Z])(?=.*\d).+$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;

    public UserService(AppDbContext db, ICurrentUserService currentUser, IAuditLogService auditLog)
    {
        _db = db;
        _currentUser = currentUser;
        _auditLog = auditLog;
    }

    // ---------- Self-service (me/**) ----------

    public async Task<UserDetailResponse> GetMyProfileAsync()
    {
        var user = await FindOrThrowAsync(CurrentUserId);
        return ToResponse(user);
    }

    public async Task<UserDetailResponse> UpdateMyProfileAsync(UpdateProfileRequest request)
    {
        var user = await FindOrThrowAsync(CurrentUserId);

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return ToResponse(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = await FindOrThrowAsync(CurrentUserId);

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
        {
            throw AppException.Unauthorized("USER_OLD_PASSWORD_INVALID", "Mật khẩu cũ không đúng");
        }

        if (!PasswordPolicyRegex.IsMatch(request.NewPassword))
        {
            throw AppException.Unprocessable("USER_PASSWORD_POLICY",
                "Password phải có tối thiểu 8 ký tự, ít nhất 1 chữ hoa và 1 chữ số");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // [Assumption] SRS không đặc tả có hủy session cũ sau khi đổi mật khẩu hay không (Phase 9 plan).
        // Giữ nguyên session hiện tại — nằm ngoài phạm vi UserService (Cookie Auth do AuthController quản lý).
        await _db.SaveChangesAsync();
    }

    // ---------- Admin ----------

    public async Task<PagedResult<UserDetailResponse>> GetAdminListAsync(
        string? search, short? role, bool? status, int page, int pageSize)
    {
        var query = _db.Users.Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(u => EF.Functions.ILike(u.Email, $"%{keyword}%")
                || EF.Functions.ILike(u.FullName, $"%{keyword}%"));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.RoleId == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.IsLocked == status.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => ToResponse(u))
            .ToListAsync();

        return new PagedResult<UserDetailResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<UserDetailResponse> GetAdminDetailAsync(long id)
    {
        var user = await FindOrThrowAsync(id);
        return ToResponse(user);
    }

    public async Task<UserDetailResponse> LockUserAsync(long targetUserId, bool isLocked)
    {
        var user = await FindOrThrowAsync(targetUserId);

        // BR-14 / FR-USER-009: Admin không được TỰ KHÓA chính tài khoản đang đăng nhập của mình.
        // So sánh targetUserId (từ URL) với ICurrentUserService.UserId (từ Claims đã xác thực) — không bao giờ
        // tin id "mình" do client tự khai trong body (SEC-10).
        // Chỉ chặn khi isLocked=true: rule chỉ nói "không được tự khóa", không nói gì về tự mở khóa.
        // Đọc literal: Admin tự MỞ khóa chính mình không vi phạm BR-14 (và trên thực tế hiếm khi xảy ra vì
        // một Admin đã bị khóa sẽ không đăng nhập được để tự mở khóa) nên KHÔNG chặn trường hợp này.
        if (isLocked && targetUserId == CurrentUserId)
        {
            throw AppException.Conflict("CANNOT_LOCK_SELF", "Không thể tự khóa tài khoản đang đăng nhập của chính mình");
        }

        user.IsLocked = isLocked;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(
            isLocked ? AuditAction.UserLock : AuditAction.UserUnlock,
            EntityType.User,
            user.Id,
            new { user.IsLocked });

        return ToResponse(user);
    }

    public async Task<UserDetailResponse> ChangeRoleAsync(long targetUserId, short roleId)
    {
        var user = await FindOrThrowAsync(targetUserId);

        var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId);
        if (!roleExists)
        {
            throw AppException.Unprocessable("USER_ROLE_INVALID", "Role không tồn tại");
        }

        user.RoleId = roleId;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(AuditAction.UserRoleChange, EntityType.User, user.Id, new { user.RoleId });

        return ToResponse(user);
    }

    // ---------- Helpers ----------

    private long CurrentUserId => _currentUser.UserId
        ?? throw AppException.Unauthorized("USER_NOT_AUTHENTICATED", "Chưa đăng nhập");

    private async Task<User> FindOrThrowAsync(long id)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            throw AppException.NotFound("USER_NOT_FOUND", "Không tìm thấy người dùng.");
        }

        return user;
    }

    private static UserDetailResponse ToResponse(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        Phone = u.Phone,
        Role = u.Role.Name,
        IsLocked = u.IsLocked,
        CreatedAt = u.CreatedAt
    };
}
