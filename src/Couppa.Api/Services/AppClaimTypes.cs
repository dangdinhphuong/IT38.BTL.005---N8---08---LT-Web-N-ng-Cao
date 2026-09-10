namespace Couppa.Api.Services;

/// <summary>Tên Claim dùng trong ClaimsPrincipal sau khi đăng nhập (Cookie Auth).</summary>
public static class AppClaimTypes
{
    public const string UserId = "couppa:user_id";
    public const string Role = "couppa:role";
}
