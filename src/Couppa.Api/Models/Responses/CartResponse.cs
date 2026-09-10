namespace Couppa.Api.Models.Responses;

public class CartResponse
{
    public required IReadOnlyList<CartItemResponse> Items { get; init; }

    /// <summary>Tổng số lượng, chỉ tính các item IsAvailable=true (BR-11).</summary>
    public required int TotalQuantity { get; init; }

    /// <summary>Tạm tính, chỉ tính các item IsAvailable=true (BR-11).</summary>
    public required decimal Subtotal { get; init; }
}
