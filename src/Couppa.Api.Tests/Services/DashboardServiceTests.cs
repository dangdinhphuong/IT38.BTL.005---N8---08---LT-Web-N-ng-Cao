using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Couppa.Api.Tests.Services;

public class DashboardServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateTestConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cache:CategoriesActiveTtlMinutes"] = "15",
            ["Cache:ProductsFeaturedTtlMinutes"] = "10",
            ["Cache:ProductsLatestTtlMinutes"] = "5",
            ["Cache:DashboardSummaryTtlMinutes"] = "5"
        })
        .Build();

    private static DashboardService CreateService(AppDbContext db, out ICacheService cache)
    {
        // Cache thật (không mock): mỗi test dùng 1 MemoryCache riêng, tránh rò rỉ trạng thái cache giữa các test.
        cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        return new DashboardService(db, cache, CreateTestConfiguration());
    }

    private static Category NewCategory(string name, string slug) => new()
    {
        Name = name,
        Slug = slug,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static Product NewProduct(
        string sku, string name, long categoryId, int stock = 10, bool isActive = true, DateTimeOffset? createdAt = null) => new()
    {
        Sku = sku,
        Name = name,
        CategoryId = categoryId,
        Price = 100,
        StockQuantity = stock,
        IsActive = isActive,
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static User NewUser(string email) => new()
    {
        Email = email,
        PasswordHash = "hash",
        FullName = "Test User",
        RoleId = RoleIds.User,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task GetSummaryAsync_EmptyDatabase_ReturnsZeroAndEmptyLists_DoesNotThrow()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, out _);

        var result = await service.GetSummaryAsync();

        Assert.Equal(0, result.TotalProducts);
        Assert.Equal(0, result.TotalCategories);
        Assert.Equal(0, result.TotalUsers);
        Assert.Equal(0, result.ActiveProducts);
        Assert.Equal(0, result.OutOfStockProducts);
        Assert.Empty(result.ProductsByCategory);
        Assert.Empty(result.RecentProducts);
    }

    [Fact]
    public async Task GetSummaryAsync_WithData_ComputesAllSevenFieldsCorrectly()
    {
        await using var db = CreateDbContext();

        var cat1 = NewCategory("Điện thoại", "dien-thoai");
        var cat2 = NewCategory("Phụ kiện", "phu-kien");
        db.Categories.AddRange(cat1, cat2);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            NewProduct("SKU-1", "SP 1", cat1.Id, stock: 5, isActive: true),
            NewProduct("SKU-2", "SP 2", cat1.Id, stock: 0, isActive: true),
            NewProduct("SKU-3", "SP 3", cat2.Id, stock: 0, isActive: false));
        db.Users.AddRange(NewUser("a@test.com"), NewUser("b@test.com"));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _);

        var result = await service.GetSummaryAsync();

        Assert.Equal(3, result.TotalProducts);
        Assert.Equal(2, result.TotalCategories);
        Assert.Equal(2, result.TotalUsers);
        Assert.Equal(2, result.ActiveProducts);
        Assert.Equal(2, result.OutOfStockProducts);
        Assert.Equal(2, result.ProductsByCategory.Count);
        Assert.Contains(result.ProductsByCategory, c => c.CategoryName == "Điện thoại" && c.Count == 2);
        Assert.Contains(result.ProductsByCategory, c => c.CategoryName == "Phụ kiện" && c.Count == 1);
    }

    [Fact]
    public async Task GetSummaryAsync_RecentProducts_ReturnsTop5OrderedByCreatedAtDescending()
    {
        await using var db = CreateDbContext();

        var cat = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 7; i++)
        {
            db.Products.Add(NewProduct($"SKU-{i}", $"SP {i}", cat.Id, createdAt: now.AddMinutes(-i)));
        }
        await db.SaveChangesAsync();

        var service = CreateService(db, out _);

        var result = await service.GetSummaryAsync();

        Assert.Equal(5, result.RecentProducts.Count);
        Assert.Equal("SP 0", result.RecentProducts[0].Name);
        Assert.Equal("SP 4", result.RecentProducts[4].Name);
    }

    [Fact]
    public async Task GetSummaryAsync_SecondCallWithinTtl_ReturnsFromCache_DoesNotReflectNewData()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, out _);

        var first = await service.GetSummaryAsync();
        Assert.Equal(0, first.TotalCategories);

        // Thêm dữ liệu mới sau lần gọi đầu - vì cache admin:dashboard:summary chưa hết TTL 5 phút,
        // lần gọi thứ 2 phải vẫn trả kết quả cũ từ cache, không query lại DB (task 10, AC FR-DASH-001).
        db.Categories.Add(NewCategory("Điện thoại", "dien-thoai"));
        await db.SaveChangesAsync();

        var second = await service.GetSummaryAsync();
        Assert.Equal(0, second.TotalCategories);
    }
}
