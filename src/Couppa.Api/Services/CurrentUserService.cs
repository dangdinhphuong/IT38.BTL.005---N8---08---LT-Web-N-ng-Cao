using System.Security.Claims;

namespace Couppa.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private const string GuestSessionKey = "couppa:guest_session_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext Context => _httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("Không có HttpContext hiện tại.");

    public bool IsAuthenticated => Context.User.Identity?.IsAuthenticated ?? false;

    // Identity gắn UserId (GUID string) vào claim chuẩn ClaimTypes.NameIdentifier khi SignInManager đăng nhập.
    public string? UserId => Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public bool IsAdmin => Context.User.IsInRole("Admin");

    public Guid GetOrCreateGuestSessionId()
    {
        var session = Context.Session;
        var existing = session.GetString(GuestSessionKey);
        if (existing is not null && Guid.TryParse(existing, out var parsed))
        {
            return parsed;
        }

        var newId = Guid.NewGuid();
        session.SetString(GuestSessionKey, newId.ToString());
        return newId;
    }
}
