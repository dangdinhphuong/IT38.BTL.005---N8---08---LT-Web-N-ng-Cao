namespace Couppa.Api.Models.Responses;

/// <summary>
/// Dùng cho self-service profile (/api/users/me) và Admin xem danh sách/chi tiết user.
/// SEC-12: không bao giờ chứa PasswordHash.
/// </summary>
public class UserDetailResponse
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public string? Phone { get; init; }
    public required string Role { get; init; }
    public required bool IsLocked { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
