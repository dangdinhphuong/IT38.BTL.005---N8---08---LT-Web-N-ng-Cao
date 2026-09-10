using System.ComponentModel.DataAnnotations;

namespace Couppa.Api.Models.Requests;

public class UpdateCartItemRequest
{
    // SEC-06: quantity phải >= 1 ngay ở model binding (giảm về 0 phải dùng DELETE, không PUT).
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public required int Quantity { get; init; }
}
