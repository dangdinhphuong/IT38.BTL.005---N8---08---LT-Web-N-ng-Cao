namespace Couppa.Api.Models.Responses;

public class ProductResponse
{
    public required long Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required long CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public required decimal Price { get; init; }
    public required int StockQuantity { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsFeatured { get; init; }

    /// <summary>BR-01 — Cart Service dùng field này để biết sản phẩm còn thêm được vào giỏ không.</summary>
    public required bool IsAvailableForCart { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required IReadOnlyList<ProductImageResponse> Images { get; init; }
}
