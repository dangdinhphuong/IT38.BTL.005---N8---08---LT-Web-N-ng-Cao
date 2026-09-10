using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

/// <summary>
/// DD-02 / FR-CART-001..008. Giỏ hàng luôn lưu DB, kể cả của Guest (định danh qua Cart.SessionId).
/// Dùng trực tiếp AppDbContext.Products (thay vì IProductService) để luôn đọc StockQuantity MỚI NHẤT
/// tại đúng thời điểm ghi cart_items, tránh dữ liệu cache/stale khi có nhiều request đồng thời.
/// </summary>
public class CartService : ICartService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CartService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CartResponse> GetCurrentCartAsync()
    {
        var cart = await FindCurrentCartAsync();
        if (cart is null)
        {
            // Chưa từng có cart -> trả giỏ rỗng, không cần tạo record DB chỉ vì 1 lượt GET.
            return new CartResponse { Items = Array.Empty<CartItemResponse>(), TotalQuantity = 0, Subtotal = 0 };
        }

        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> AddItemAsync(AddCartItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            throw AppException.Unprocessable("CART_QUANTITY_INVALID", "Số lượng phải lớn hơn 0.");
        }

        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null)
        {
            throw AppException.NotFound("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");
        }

        // BR-01: sản phẩm phải Active và còn tồn kho mới được thêm vào giỏ.
        if (!product.IsAvailableForCart)
        {
            throw AppException.Conflict("PRODUCT_UNAVAILABLE", "Sản phẩm hiện không khả dụng.");
        }

        var cart = await GetOrCreateCurrentCartAsync();

        var existingItem = await _db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == request.ProductId);

        if (existingItem is null)
        {
            // Thêm MỚI: nếu quantity vượt tồn kho ngay từ đầu -> throw Conflict (khác với merge, không được
            // silently cắt ở đây - task 07 API spec 409 "quantity vượt tồn kho", phân biệt rõ với BR-13/merge).
            if (request.Quantity > product.StockQuantity)
            {
                throw AppException.Conflict("CART_QUANTITY_EXCEEDS_STOCK", "Số lượng vượt quá tồn kho hiện tại.");
            }

            _db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            // Alternative Flow UC-02: sản phẩm đã có trong giỏ -> cộng dồn nhưng không vượt tồn kho hiện tại.
            var desired = existingItem.Quantity + request.Quantity;
            if (desired > product.StockQuantity)
            {
                throw AppException.Conflict("CART_QUANTITY_EXCEEDS_STOCK", "Số lượng vượt quá tồn kho hiện tại.");
            }

            existingItem.Quantity = desired;
            existingItem.UpdatedAt = DateTimeOffset.UtcNow;
        }

        cart.UpdatedAt = DateTimeOffset.UtcNow;

        LogActivity(CartActivityAction.Add, request.ProductId, cart, request.Quantity);

        await _db.SaveChangesAsync();

        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> UpdateItemQuantityAsync(long itemId, UpdateCartItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            throw AppException.Unprocessable("CART_QUANTITY_INVALID", "Số lượng phải >= 1 (giảm về 0, dùng DELETE).");
        }

        var cart = await FindCurrentCartAsync();
        var item = cart is null ? null : await _db.CartItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id);

        if (item is null)
        {
            throw AppException.NotFound("CART_ITEM_NOT_FOUND", "Không tìm thấy sản phẩm trong giỏ hàng.");
        }

        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.ProductId);
        var stockQuantity = product?.StockQuantity ?? 0;

        // BR-02: số lượng không được vượt tồn kho hiện tại tại thời điểm ghi.
        if (request.Quantity > stockQuantity)
        {
            throw AppException.Conflict("CART_QUANTITY_EXCEEDS_STOCK", "Số lượng vượt quá tồn kho hiện tại.");
        }

        item.Quantity = request.Quantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        cart!.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return await BuildResponseAsync(cart.Id);
    }

    public async Task<CartResponse> RemoveItemAsync(long itemId)
    {
        var cart = await FindCurrentCartAsync();
        var item = cart is null ? null : await _db.CartItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id);

        if (item is null)
        {
            throw AppException.NotFound("CART_ITEM_NOT_FOUND", "Không tìm thấy sản phẩm trong giỏ hàng.");
        }

        _db.CartItems.Remove(item);
        cart!.UpdatedAt = DateTimeOffset.UtcNow;

        LogActivity(CartActivityAction.Remove, item.ProductId, cart, item.Quantity);

        await _db.SaveChangesAsync();

        return await BuildResponseAsync(cart.Id);
    }

    public async Task ClearCartAsync()
    {
        var cart = await FindCurrentCartAsync();
        if (cart is null)
        {
            return;
        }

        var items = await _db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync();
        if (items.Count == 0)
        {
            return;
        }

        _db.CartItems.RemoveRange(items);
        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// FR-CART-007 / BR-13. 4 trường hợp (bảng Phase 7 của plan triển khai):
    ///   a) Không Guest cart, không User cart -> tạo cart rỗng cho user.
    ///   b) Có Guest cart, không User cart    -> đổi chủ sở hữu NGAY trên cart Guest đó (UPDATE, không tạo mới).
    ///   c) Không Guest cart, có User cart    -> giữ nguyên cart User, không làm gì.
    ///   d) Có cả 2                            -> merge từng CartItem theo ProductId, cộng dồn, CẮT theo tồn kho
    ///      hiện tại (không throw - merge phải "êm", không được làm fail cả luồng login), sau đó xóa cart Guest.
    /// </summary>
    public async Task<CartResponse> MergeGuestCartAsync(Guid guestSessionId, string userId)
    {
        var guestCart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == guestSessionId);
        var userCart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (guestCart is null && userCart is null)
        {
            // (a) Không có gì để merge -> tạo cart rỗng cho user để các thao tác sau có sẵn cart.
            var newCart = new Cart
            {
                UserId = userId,
                SessionId = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.Carts.Add(newCart);
            await _db.SaveChangesAsync();

            return await BuildResponseAsync(newCart.Id);
        }

        if (guestCart is not null && userCart is null)
        {
            // (b) Chỉ có Guest cart -> cách nhanh nhất là đổi chủ sở hữu ngay trên cart hiện có
            // (UPDATE session_id=null, user_id=userId), giữ nguyên toàn bộ CartItem con, không tạo cart mới.
            guestCart.UserId = userId;
            guestCart.SessionId = null;
            guestCart.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            return await BuildResponseAsync(guestCart.Id);
        }

        if (guestCart is null)
        {
            // (c) Chỉ có User cart sẵn có -> giữ nguyên, không làm gì thêm.
            return await BuildResponseAsync(userCart!.Id);
        }

        // (d) Có cả 2 cart -> merge từng CartItem của Guest vào User theo ProductId.
        // IgnoreQueryFilters: cần biết StockQuantity kể cả của sản phẩm đã soft-delete (BR-11 áp dụng
        // xuyên suốt cả khi merge) - nếu không, GetValueOrDefault sẽ ngầm hiểu "không tồn tại" -> stock=0
        // -> có thể tạo/giữ item với Quantity=0 (vi phạm CHECK ck_cart_items_quantity_positive).
        var productIds = guestCart.Items.Select(i => i.ProductId)
            .Union(userCart!.Items.Select(i => i.ProductId))
            .ToList();
        var stockByProductId = await _db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsActive && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, p => p.StockQuantity);

        foreach (var guestItem in guestCart.Items.ToList())
        {
            // Sản phẩm đã bị xóa/Inactive hoặc hết hàng hoàn toàn (không có trong dictionary, hoặc stock=0):
            // không thể giữ quantity > 0 cho item merge từ Guest (CHECK constraint quantity > 0) -> bỏ qua,
            // item Guest cart tương ứng sẽ mất theo cùng việc xóa cart Guest bên dưới. Item phía User cart
            // (nếu có) giữ nguyên nguyên trạng, không đụng tới.
            if (!stockByProductId.TryGetValue(guestItem.ProductId, out var stock) || stock <= 0)
            {
                continue;
            }

            var userItem = userCart.Items.FirstOrDefault(i => i.ProductId == guestItem.ProductId);

            if (userItem is not null)
            {
                // Trùng sản phẩm: cộng dồn rồi CẮT theo tồn kho hiện tại (BR-13) - không throw, merge phải êm.
                var merged = userItem.Quantity + guestItem.Quantity;
                userItem.Quantity = Math.Min(merged, stock);
                userItem.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                // Sản phẩm chỉ có ở Guest cart: chuyển thẳng sang User cart, cắt theo tồn kho nếu vượt.
                _db.CartItems.Add(new CartItem
                {
                    CartId = userCart.Id,
                    ProductId = guestItem.ProductId,
                    Quantity = Math.Min(guestItem.Quantity, stock),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        // Xóa cart Guest sau khi merge xong - cascade xóa luôn CartItem cũ của nó (DeleteBehavior.Cascade).
        _db.Carts.Remove(guestCart);
        userCart.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return await BuildResponseAsync(userCart.Id);
    }

    // ---- helpers ----

    private async Task<Cart?> FindCurrentCartAsync()
    {
        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId!;
            return await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        var sessionId = _currentUser.GetOrCreateGuestSessionId();
        return await _db.Carts.FirstOrDefaultAsync(c => c.SessionId == sessionId);
    }

    private async Task<Cart> GetOrCreateCurrentCartAsync()
    {
        var cart = await FindCurrentCartAsync();
        if (cart is not null)
        {
            return cart;
        }

        cart = _currentUser.IsAuthenticated
            ? new Cart { UserId = _currentUser.UserId!, SessionId = null }
            : new Cart { UserId = null, SessionId = _currentUser.GetOrCreateGuestSessionId() };

        cart.CreatedAt = DateTimeOffset.UtcNow;
        cart.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();

        return cart;
    }

    private void LogActivity(string action, long productId, Cart cart, int quantity)
    {
        _db.CartActivityLogs.Add(new CartActivityLog
        {
            ProductId = productId,
            UserId = cart.UserId,
            SessionId = cart.SessionId,
            Action = action,
            Quantity = quantity,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private async Task<CartResponse> BuildResponseAsync(long cartId)
    {
        var items = await _db.CartItems
            .Where(i => i.CartId == cartId)
            .OrderBy(i => i.Id)
            .ToListAsync();

        if (items.Count == 0)
        {
            return new CartResponse { Items = Array.Empty<CartItemResponse>(), TotalQuantity = 0, Subtotal = 0 };
        }

        var productIds = items.Select(i => i.ProductId).ToList();

        // IgnoreQueryFilters: BR-11 cần biết cả sản phẩm đã soft-delete để đánh dấu isAvailable=false,
        // Global Query Filter (IsDeleted) sẽ loại chúng khỏi kết quả nếu không bỏ qua ở đây.
        var products = await _db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Include(p => p.Images)
            .ToDictionaryAsync(p => p.Id, p => p);

        var itemResponses = new List<CartItemResponse>(items.Count);
        foreach (var item in items)
        {
            products.TryGetValue(item.ProductId, out var product);

            // BR-11: sản phẩm đã xóa hoặc Inactive -> item vẫn giữ trong DB nhưng đánh dấu không khả dụng,
            // hệ thống KHÔNG tự xóa item, loại khỏi subtotal/totalQuantity tổng của giỏ.
            var isAvailable = product is not null && product.IsActive && !product.IsDeleted;
            var price = product?.Price ?? 0m;
            var primaryImageUrl = product?.Images
                .OrderByDescending(img => img.IsPrimary)
                .ThenBy(img => img.SortOrder)
                .FirstOrDefault()?.ImageUrl;

            itemResponses.Add(new CartItemResponse
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = product?.Name ?? "Sản phẩm không còn tồn tại",
                ProductImageUrl = primaryImageUrl,
                Price = price,
                Quantity = item.Quantity,
                Subtotal = price * item.Quantity,
                IsAvailable = isAvailable
            });
        }

        var availableItems = itemResponses.Where(i => i.IsAvailable).ToList();

        return new CartResponse
        {
            Items = itemResponses,
            TotalQuantity = availableItems.Sum(i => i.Quantity),
            Subtotal = availableItems.Sum(i => i.Subtotal)
        };
    }
}
