namespace Couppa.Api.Models.Responses;

public class CategoryResponse
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Số sản phẩm (chưa xóa) thuộc danh mục — dùng hiển thị badge ở menu/sidebar filter.</summary>
    public int ProductCount { get; init; }
}
