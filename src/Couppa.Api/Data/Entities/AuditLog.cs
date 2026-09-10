namespace Couppa.Api.Data.Entities;

public static class AuditAction
{
    public const string ProductCreate = "PRODUCT_CREATE";
    public const string ProductUpdate = "PRODUCT_UPDATE";
    public const string ProductDelete = "PRODUCT_DELETE";
    public const string ProductStatusChange = "PRODUCT_STATUS_CHANGE";
    public const string CategoryCreate = "CATEGORY_CREATE";
    public const string CategoryUpdate = "CATEGORY_UPDATE";
    public const string CategoryDelete = "CATEGORY_DELETE";
    public const string CategoryStatusChange = "CATEGORY_STATUS_CHANGE";
    public const string UserLock = "USER_LOCK";
    public const string UserUnlock = "USER_UNLOCK";
    public const string UserRoleChange = "USER_ROLE_CHANGE";
}

public static class EntityType
{
    public const string Product = "Product";
    public const string Category = "Category";
    public const string User = "User";
}

public class AuditLog
{
    public long Id { get; set; }
    public long? ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public long? EntityId { get; set; }

    /// <summary>Chỉ chứa trường nghiệp vụ đã thay đổi — không bao giờ chứa password (SRS Chương 20).</summary>
    public string? DetailJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
