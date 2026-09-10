namespace Couppa.Api.Data.Entities;

public class User
{
    public long Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public short RoleId { get; set; } = RoleIds.User;
    public bool IsLocked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public Cart? Cart { get; set; }
}
