namespace Couppa.Api.Models.Responses;

/// <summary>SEC-12: không bao giờ chứa password_hash.</summary>
public class UserResponse
{
    public required long Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public required string Role { get; init; }
}
