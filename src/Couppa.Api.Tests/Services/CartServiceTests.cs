using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Couppa.Api.Tests.Services;

/// <summary>
/// Phần phức tạp nhất của hệ thống (Phase 7 - Cart). Trọng tâm: BR-01/BR-02 (add/update) và
/// MergeGuestCartAsync (BR-13) đủ 4 trường hợp a/b/c/d theo bảng ở plan Phase 7.
/// </summary>
public class CartServiceTests
{
    private static readonly Guid FixedGuestSessionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string FixedUserId = "user-100";

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Mock<ICurrentUserService> CreateGuestCurrentUser()
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(c => c.IsAuthenticated).Returns(false);
        mock.SetupGet(c => c.UserId).Returns((string?)null);
        mock.Setup(c => c.GetOrCreateGuestSessionId()).Returns(FixedGuestSessionId);
        return mock;
    }

    private static Mock<ICurrentUserService> CreateUserCurrentUser(string userId = FixedUserId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(c => c.IsAuthenticated).Returns(true);
        mock.SetupGet(c => c.UserId).Returns(userId);
        return mock;
    }

    private static CartService CreateService(AppDbContext db, ICurrentUserService currentUser) =>
        new(db, currentUser);

    private static Category NewCategory(long id = 1) => new()
    {
        Id = id,
        Name = "Điện thoại",
        Slug = "dien-thoai",
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static Product NewProduct(
        long id, string name, int stockQuantity, bool isActive = true, bool isDeleted = false, long categoryId = 1) => new()
    {
        Id = id,
        Sku = $"SKU-{id}",
        Name = name,
        CategoryId = categoryId,
        Price = 100_000m,
        StockQuantity = stockQuantity,
        IsActive = isActive,
        IsDeleted = isDeleted,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    // ---------- AddItemAsync: BR-01 ----------

    [Fact]
    public async Task AddItemAsync_ProductInactive_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 10, isActive: false));
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateGuestCurrentUser().Object);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.AddItemAsync(new AddCartItemRequest { ProductId = 1, Quantity = 1 }));

        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PRODUCT_UNAVAILABLE", ex.Code);
        Assert.Empty(db.CartItems);
    }

    [Fact]
    public async Task AddItemAsync_ProductOutOfStock_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 0));
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateGuestCurrentUser().Object);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.AddItemAsync(new AddCartItemRequest { ProductId = 1, Quantity = 1 }));

        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PRODUCT_UNAVAILABLE", ex.Code);
        Assert.Empty(db.CartItems);
    }

    // ---------- AddItemAsync: BR-02 (thêm mới vượt tồn kho ngay từ đầu -> throw, khác merge) ----------

    [Fact]
    public async Task AddItemAsync_QuantityExceedsStockOnFreshCart_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 3));
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateGuestCurrentUser().Object);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.AddItemAsync(new AddCartItemRequest { ProductId = 1, Quantity = 5 }));

        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("CART_QUANTITY_EXCEEDS_STOCK", ex.Code);
        Assert.Empty(db.CartItems);
    }

    // ---------- AddItemAsync: happy path ----------

    [Fact]
    public async Task AddItemAsync_ValidProduct_GuestCartNotExists_CreatesCartWithSessionIdAndItem()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 10));
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateGuestCurrentUser().Object);

        var result = await service.AddItemAsync(new AddCartItemRequest { ProductId = 1, Quantity = 2 });

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Quantity);
        Assert.Equal(2, result.TotalQuantity);
        Assert.True(result.Items[0].IsAvailable);

        var cart = await db.Carts.SingleAsync();
        Assert.Null(cart.UserId);
        Assert.Equal(FixedGuestSessionId, cart.SessionId);

        // Mỗi AddItem thành công phải ghi đúng 1 dòng cart_activity_logs (action=add).
        var log = await db.CartActivityLogs.SingleAsync();
        Assert.Equal(CartActivityAction.Add, log.Action);
        Assert.Equal(1L, log.ProductId);
        Assert.Equal(2, log.Quantity);
        Assert.Equal(FixedGuestSessionId, log.SessionId);
        Assert.Null(log.UserId);
    }

    [Fact]
    public async Task AddItemAsync_ProductAlreadyInCart_AccumulatesQuantity()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 10));
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateGuestCurrentUser().Object);

        await service.AddItemAsync(new AddCartItemRequest { ProductId = 1, Quantity = 2 });
        var result = await service.AddItemAsync(new AddCartItemRequest { ProductId = 1, Quantity = 3 });

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Quantity);

        // Không được tạo thêm dòng cart_items thứ 2 (UPSERT, không INSERT mù).
        Assert.Single(db.CartItems);
        // 2 lần add thành công -> 2 dòng log.
        Assert.Equal(2, await db.CartActivityLogs.CountAsync());
    }

    // ---------- UpdateItemQuantityAsync: BR-02 ----------

    [Fact]
    public async Task UpdateItemQuantityAsync_ExceedsStock_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 5));
        await db.SaveChangesAsync();

        var currentUser = CreateGuestCurrentUser();
        var service = CreateService(db, currentUser.Object);
        await service.AddItemAsync(new AddCartItemRequest { ProductId = 1, Quantity = 2 });
        var item = await db.CartItems.SingleAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateItemQuantityAsync(item.Id, new UpdateCartItemRequest { Quantity = 6 }));

        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("CART_QUANTITY_EXCEEDS_STOCK", ex.Code);

        // Quantity không bị thay đổi sau khi throw.
        var unchanged = await db.CartItems.SingleAsync();
        Assert.Equal(2, unchanged.Quantity);
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_RemovesAndLogsActivity()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 5));
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateGuestCurrentUser().Object);
        await service.AddItemAsync(new AddCartItemRequest { ProductId = 1, Quantity = 2 });
        var item = await db.CartItems.SingleAsync();

        var result = await service.RemoveItemAsync(item.Id);

        Assert.Empty(result.Items);
        Assert.Empty(db.CartItems);

        var removeLog = await db.CartActivityLogs.SingleAsync(l => l.Action == CartActivityAction.Remove);
        Assert.Equal(1L, removeLog.ProductId);
        Assert.Equal(2, removeLog.Quantity);
    }

    // ---------- MergeGuestCartAsync: 4 trường hợp BR-13 ----------

    [Fact]
    public async Task MergeGuestCartAsync_CaseA_NeitherCartExists_CreatesEmptyCartForUser()
    {
        await using var db = CreateDbContext();

        var service = CreateService(db, CreateUserCurrentUser().Object);

        var result = await service.MergeGuestCartAsync(FixedGuestSessionId, FixedUserId);

        Assert.Empty(result.Items);

        var cart = await db.Carts.SingleAsync();
        Assert.Equal(FixedUserId, cart.UserId);
        Assert.Null(cart.SessionId);
    }

    [Fact]
    public async Task MergeGuestCartAsync_CaseB_OnlyGuestCartExists_ReassignsOwnerToUser()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 10));
        db.Products.Add(NewProduct(2, "Sản phẩm B", stockQuantity: 10));
        var guestCart = new Cart
        {
            SessionId = FixedGuestSessionId,
            UserId = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Carts.Add(guestCart);
        await db.SaveChangesAsync();
        db.CartItems.AddRange(
            new CartItem { CartId = guestCart.Id, ProductId = 1, Quantity = 2, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new CartItem { CartId = guestCart.Id, ProductId = 2, Quantity = 1, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateUserCurrentUser().Object);

        var result = await service.MergeGuestCartAsync(FixedGuestSessionId, FixedUserId);

        Assert.Equal(2, result.Items.Count);

        // Đúng 1 cart trong DB - cart Guest được ĐỔI chủ (UPDATE), không tạo cart mới.
        var cart = await db.Carts.SingleAsync();
        Assert.Equal(guestCart.Id, cart.Id);
        Assert.Equal(FixedUserId, cart.UserId);
        Assert.Null(cart.SessionId);
        Assert.Equal(2, await db.CartItems.CountAsync(i => i.CartId == cart.Id));
    }

    [Fact]
    public async Task MergeGuestCartAsync_CaseC_OnlyUserCartExists_KeepsUserCartUnchanged()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 10));
        var userCart = new Cart
        {
            UserId = FixedUserId,
            SessionId = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Carts.Add(userCart);
        await db.SaveChangesAsync();
        db.CartItems.Add(new CartItem { CartId = userCart.Id, ProductId = 1, Quantity = 4, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateUserCurrentUser().Object);

        var result = await service.MergeGuestCartAsync(FixedGuestSessionId, FixedUserId);

        Assert.Single(result.Items);
        Assert.Equal(4, result.Items[0].Quantity);

        var cart = await db.Carts.SingleAsync();
        Assert.Equal(userCart.Id, cart.Id);
        Assert.Single(await db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync());
    }

    [Fact]
    public async Task MergeGuestCartAsync_CaseD_BothCartsExist_MergesAndClampsToStock_DeletesGuestCart()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory());
        // ProductA: tồn kho = 5. Guest có 3, User có sẵn 4 -> tổng 7 phải bị CẮT xuống 5 (BR-13/BR-02).
        db.Products.Add(NewProduct(1, "Sản phẩm A", stockQuantity: 5));
        // ProductB: chỉ ở Guest cart, tồn kho đủ -> chuyển nguyên qua User.
        db.Products.Add(NewProduct(2, "Sản phẩm B", stockQuantity: 10));

        var guestCart = new Cart { SessionId = FixedGuestSessionId, UserId = null, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        var userCart = new Cart { UserId = FixedUserId, SessionId = null, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        db.Carts.AddRange(guestCart, userCart);
        await db.SaveChangesAsync();

        db.CartItems.AddRange(
            new CartItem { CartId = guestCart.Id, ProductId = 1, Quantity = 3, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new CartItem { CartId = guestCart.Id, ProductId = 2, Quantity = 2, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new CartItem { CartId = userCart.Id, ProductId = 1, Quantity = 4, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateUserCurrentUser().Object);

        var result = await service.MergeGuestCartAsync(FixedGuestSessionId, FixedUserId);

        Assert.Equal(2, result.Items.Count);

        var productAItem = result.Items.Single(i => i.ProductId == 1);
        var productBItem = result.Items.Single(i => i.ProductId == 2);

        // 3 (guest) + 4 (user) = 7, nhưng tồn kho chỉ 5 -> phải cắt xuống đúng 5, không được vượt (BR-02).
        Assert.Equal(5, productAItem.Quantity);
        // ProductB chuyển nguyên từ Guest sang, tồn kho đủ -> giữ nguyên 2.
        Assert.Equal(2, productBItem.Quantity);

        // Cart Guest phải bị xóa khỏi DB sau merge (kiểm tra trực tiếp bằng query, không chỉ dựa vào response).
        var guestCartStillExists = await db.Carts.AnyAsync(c => c.Id == guestCart.Id);
        Assert.False(guestCartStillExists);
        Assert.Empty(await db.CartItems.Where(i => i.CartId == guestCart.Id).ToListAsync());

        // Cart User vẫn còn đúng 1 cart duy nhất trong DB sau merge.
        var remainingCart = await db.Carts.SingleAsync();
        Assert.Equal(userCart.Id, remainingCart.Id);
        Assert.Equal(FixedUserId, remainingCart.UserId);
    }
}
