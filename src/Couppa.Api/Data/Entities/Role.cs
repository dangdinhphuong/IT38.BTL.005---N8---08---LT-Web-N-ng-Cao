namespace Couppa.Api.Data.Entities;

public static class RoleIds
{
    public const short User = 1;
    public const short Admin = 2;
}

public class Role
{
    public short Id { get; set; }
    public required string Name { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
