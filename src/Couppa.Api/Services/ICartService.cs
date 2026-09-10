using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;

namespace Couppa.Api.Services;

/// <summary>
/// DD-02: giỏ hàng luôn lưu DB (kể cả Guest). Guest/User hiện tại được xác định qua
/// ICurrentUserService (Session cho Guest, Claims cho User) — mọi method dưới đây tự suy ra
/// giỏ hàng đang thao tác, không nhận userId/sessionId trực tiếp từ tham số (trừ MergeGuestCartAsync).
/// </summary>
public interface ICartService
{
    /// <summary>GET /api/cart (FR-CART-002).</summary>
    Task<CartResponse> GetCurrentCartAsync();

    /// <summary>POST /api/cart/items (FR-CART-001, UC-02). Validate BR-01/BR-02, ghi cart_activity_logs (action=add).</summary>
    Task<CartResponse> AddItemAsync(AddCartItemRequest request);

    /// <summary>PUT /api/cart/items/{id} (FR-CART-003/004). Validate BR-02, kiểm tra ownership item.</summary>
    Task<CartResponse> UpdateItemQuantityAsync(long itemId, UpdateCartItemRequest request);

    /// <summary>DELETE /api/cart/items/{id} (FR-CART-005). Ghi cart_activity_logs (action=remove).</summary>
    Task<CartResponse> RemoveItemAsync(long itemId);

    /// <summary>DELETE /api/cart (FR-CART-006).</summary>
    Task ClearCartAsync();

    /// <summary>
    /// FR-CART-007 / BR-13: merge giỏ hàng Guest (sessionId) vào giỏ hàng User (userId) khi đăng nhập.
    /// Được AuthController gọi ngay sau khi xác thực thành công, TRƯỚC khi session Guest bị Clear.
    /// </summary>
    Task<CartResponse> MergeGuestCartAsync(Guid guestSessionId, long userId);
}
