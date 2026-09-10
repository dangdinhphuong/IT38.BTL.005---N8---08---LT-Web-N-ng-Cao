namespace Couppa.Api.Models.Responses;

/// <summary>Dùng cho danh sách (list/featured/latest) — gọn hơn ProductResponse chi tiết.</summary>
public class ProductSummaryResponse
{
    public required long Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required long CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public required decimal Price { get; init; }
    public required int StockQuantity { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsFeatured { get; init; }
    public required bool IsAvailableForCart { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Ảnh đại diện (IsPrimary=true, hoặc ảnh đầu tiên nếu chưa chọn đại diện) — null nếu chưa có ảnh nào.</summary>
    public string? PrimaryImageUrl { get; init; }
}
