using Microsoft.AspNetCore.Identity;

namespace Couppa.Api.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public bool IsLocked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Cart? Cart { get; set; }
}
