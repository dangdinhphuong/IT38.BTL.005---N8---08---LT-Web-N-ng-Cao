namespace Couppa.Api.Services;

public interface IAuditLogService
{
    /// <summary>Ghi 1 dòng audit log. detail chỉ chứa trường nghiệp vụ thay đổi, không chứa password (SRS Chương 20).</summary>
    Task LogAsync(string action, string entityType, long? entityId, object? detail = null);
}
