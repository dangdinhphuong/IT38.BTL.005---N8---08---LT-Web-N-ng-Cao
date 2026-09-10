using System.Text.Json;
using Couppa.Api.Data;
using Couppa.Api.Data.Entities;

namespace Couppa.Api.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditLogService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string entityType, long? entityId, object? detail = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = _currentUser.IsAuthenticated ? _currentUser.UserId : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailJson = detail is null ? null : JsonSerializer.Serialize(detail),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
