namespace Couppa.Api.Data.Entities;

public static class CartActivityAction
{
    public const string Add = "add";
    public const string Remove = "remove";
}

/// <summary>
/// [Assumption A-03] — bảng bổ sung ngoài danh sách gốc SRS, bắt buộc để đáp ứng FR-REPORT-004
/// (thống kê "sản phẩm được thêm giỏ nhiều nhất"). Append-only, không update/delete.
/// </summary>
public class CartActivityLog
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public required string Action { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
