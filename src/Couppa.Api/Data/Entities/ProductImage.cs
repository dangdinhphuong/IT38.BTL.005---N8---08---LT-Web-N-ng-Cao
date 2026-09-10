namespace Couppa.Api.Data.Entities;

/// <summary>[Assumption A-02] — tách bảng riêng vì 1 sản phẩm có thể có nhiều ảnh.</summary>
public class ProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public required string ImageUrl { get; set; }
    public bool IsPrimary { get; set; }
    public short SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
