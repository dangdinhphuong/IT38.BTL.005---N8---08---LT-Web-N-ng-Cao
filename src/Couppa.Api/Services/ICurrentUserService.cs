namespace Couppa.Api.Services;

/// <summary>
/// Đọc danh tính người dùng hiện tại TỪ Claims đã xác thực của request — không bao giờ từ
/// body/query/header do client tự gửi (SEC-10). Dùng ở mọi Service cần biết "ai đang thao tác".
/// </summary>
public interface ICurrentUserService
{
    long? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }

    /// <summary>session_id định danh giỏ hàng Guest (DD-02) — đọc/ghi qua ISession, không phải Claims.</summary>
    Guid GetOrCreateGuestSessionId();
}
