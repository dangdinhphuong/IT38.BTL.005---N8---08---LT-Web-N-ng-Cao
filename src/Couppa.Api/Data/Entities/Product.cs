namespace Couppa.Api.Data.Entities;

public class Product
{
    public long Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public long CategoryId { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    /// <summary>FR-BROWSE-007 / BR-01: sản phẩm phải Active và còn tồn kho mới được thêm vào giỏ.</summary>
    public bool IsAvailableForCart => IsActive && !IsDeleted && StockQuantity > 0;
}
