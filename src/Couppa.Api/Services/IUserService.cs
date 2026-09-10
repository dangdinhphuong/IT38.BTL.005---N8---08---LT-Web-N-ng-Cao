using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;

namespace Couppa.Api.Services;

public interface IUserService
{
    /// <summary>FR-USER-006 — "tôi" xác định qua ICurrentUserService.UserId, không nhận id từ tham số.</summary>
    Task<UserDetailResponse> GetMyProfileAsync();

    /// <summary>FR-USER-007 — cập nhật fullName/phone của chính người dùng hiện tại.</summary>
    Task<UserDetailResponse> UpdateMyProfileAsync(UpdateProfileRequest request);

    /// <summary>FR-USER-008 — verify oldPassword (Unauthorized nếu sai) + validate newPassword theo BR-06 (Unprocessable nếu sai).</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request);

    /// <summary>FR-USER-001/003 — Admin xem danh sách, phân trang + search (email/tên) + filter role/status.</summary>
    Task<PagedResult<UserDetailResponse>> GetAdminListAsync(string? search, short? role, bool? status, int page, int pageSize);

    /// <summary>FR-USER-002 — Admin xem chi tiết 1 user. NotFound nếu không tồn tại.</summary>
    Task<UserDetailResponse> GetAdminDetailAsync(long id);

    /// <summary>FR-USER-004/009 — BR-14: Conflict nếu Admin tự khóa (isLocked=true) chính mình. Ghi audit_logs.</summary>
    Task<UserDetailResponse> LockUserAsync(long targetUserId, bool isLocked);

    /// <summary>FR-USER-005 — đổi role, Unprocessable nếu roleId không tồn tại. Ghi audit_logs.</summary>
    Task<UserDetailResponse> ChangeRoleAsync(long targetUserId, short roleId);
}
