using System.ComponentModel.DataAnnotations;

namespace Couppa.Api.Models.Requests;

public class AddCartItemRequest
{
    public required long ProductId { get; init; }

    // SEC-06: quantity phải >= 1 ngay ở model binding, tránh gửi 0/âm xuống Service.
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public required int Quantity { get; init; }
}
