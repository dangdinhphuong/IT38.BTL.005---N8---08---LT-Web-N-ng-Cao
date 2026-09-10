using Couppa.Api.Models.Responses;

namespace Couppa.Api.Models.ViewModels;

public class CartViewModel
{
    public CartResponse? Cart { get; set; }
    public decimal TotalAmount => Cart?.Subtotal ?? 0;
    public int TotalItems => Cart?.TotalQuantity ?? 0;
}
