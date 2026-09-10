using Couppa.Api.Data.Entities;

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

    public long? UserId
    {
        get
        {
            var claim = Context.User.FindFirst(AppClaimTypes.UserId)?.Value;
            return claim is not null && long.TryParse(claim, out var id) ? id : null;
        }
    }

    public bool IsAdmin => Context.User.FindFirst(AppClaimTypes.Role)?.Value == "Admin";

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
