namespace Couppa.Api.Models.Responses;

public class CartItemResponse
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? ProductImageUrl { get; init; }
    public required decimal Price { get; init; }
    public required int Quantity { get; init; }
    public required decimal Subtotal { get; init; }

    /// <summary>BR-11: false khi sản phẩm tham chiếu đã bị xóa (soft delete) hoặc chuyển Inactive.
    /// Item KHÔNG bị tự xóa khỏi giỏ, chỉ đánh dấu không khả dụng và bị loại khỏi subtotal/totalQuantity tổng.</summary>
    public required bool IsAvailable { get; init; }
}
