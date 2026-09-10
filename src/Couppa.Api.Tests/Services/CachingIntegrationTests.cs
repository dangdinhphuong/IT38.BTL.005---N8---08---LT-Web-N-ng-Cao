using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Couppa.Api.Tests.Services;

/// <summary>
/// Test tích hợp Task 09 (Caching): xác nhận GetOrCreateAsync thực sự phục vụ từ cache trong TTL
/// (không query lại DB) và Remove(key) sau CUD làm request kế tiếp thấy dữ liệu mới ngay lập tức.
/// Dùng MemoryCacheService thật (không mock ICacheService) + MemoryCache thật + EF Core InMemory,
/// đúng yêu cầu integration test thay vì unit test thuần theo mock.
/// </summary>
public class CachingIntegrationTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ICacheService CreateRealCache() => new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:CategoriesActiveTtlMinutes"] = "15",
                ["Cache:ProductsFeaturedTtlMinutes"] = "10",
                ["Cache:ProductsLatestTtlMinutes"] = "5",
                ["Cache:DashboardSummaryTtlMinutes"] = "5"
            })
            .Build();

    private static Mock<IAuditLogService> CreateAuditLogMock()
    {
        var mock = new Mock<IAuditLogService>();
        mock.Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Category NewCategory(string name, string slug, bool isActive = true) => new()
    {
        Name = name,
        Slug = slug,
        IsActive = isActive,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static Product NewProduct(
        string sku, string name, long categoryId, decimal price = 100, int stock = 10,
        bool isActive = true, bool isFeatured = false) => new()
    {
        Sku = sku,
        Name = name,
        CategoryId = categoryId,
        Price = price,
        StockQuantity = stock,
        IsActive = isActive,
        IsFeatured = isFeatured,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    // ---- Category ----

    [Fact]
    public async Task GetActiveListAsync_SecondCallWithinTtl_ServesFromCache_DoesNotSeeDirectDbChange()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory("Điện thoại", "dien-thoai"));
        await db.SaveChangesAsync();

        var cache = CreateRealCache();
        var service = new CategoryService(db, CreateAuditLogMock().Object, cache, CreateConfiguration());

        var firstCall = await service.GetActiveListAsync();
        Assert.Single(firstCall);

        // Thêm data thẳng vào DbContext, KHÔNG qua Service (không có invalidation nào được gọi).
        // Nếu GetOrCreateAsync query lại DB, lần gọi thứ 2 sẽ thấy 2 category thay vì 1.
        db.Categories.Add(NewCategory("Phụ kiện", "phu-kien"));
        await db.SaveChangesAsync();

        var secondCall = await service.GetActiveListAsync();

        Assert.Single(secondCall);
        Assert.Equal("dien-thoai", secondCall[0].Slug);
    }

    [Fact]
    public async Task CreateAsync_ThenGetActiveListAsync_InvalidatesCache_SeesNewCategoryImmediately()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory("Điện thoại", "dien-thoai"));
        await db.SaveChangesAsync();

        var cache = CreateRealCache();
        var service = new CategoryService(db, CreateAuditLogMock().Object, cache, CreateConfiguration());

        var firstCall = await service.GetActiveListAsync();
        Assert.Single(firstCall);

        await service.CreateAsync(new CreateCategoryRequest { Name = "Phụ kiện", Slug = "phu-kien" });

        var secondCall = await service.GetActiveListAsync();

        Assert.Equal(2, secondCall.Count);
        Assert.Contains(secondCall, c => c.Slug == "phu-kien");
    }

    [Fact]
    public async Task ChangeStatusAsync_InvalidatesCache_ActiveListReflectsChangeImmediately()
    {
        await using var db = CreateDbContext();
        var category = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var cache = CreateRealCache();
        var service = new CategoryService(db, CreateAuditLogMock().Object, cache, CreateConfiguration());

        var firstCall = await service.GetActiveListAsync();
        Assert.Single(firstCall);

        await service.ChangeStatusAsync(category.Id, false);

        var secondCall = await service.GetActiveListAsync();
        Assert.Empty(secondCall);
    }

    // ---- Product ----

    [Fact]
    public async Task GetFeaturedAsync_SecondCallWithinTtl_ServesFromCache_DoesNotSeeDirectDbChange()
    {
        await using var db = CreateDbContext();
        var category = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        db.Products.Add(NewProduct("SKU-1", "SP nổi bật 1", category.Id, isFeatured: true));
        await db.SaveChangesAsync();

        var cache = CreateRealCache();
        var categoryServiceMock = new Mock<ICategoryService>();
        var service = new ProductService(db, CreateAuditLogMock().Object, categoryServiceMock.Object, cache, CreateConfiguration());

        var firstCall = await service.GetFeaturedAsync();
        Assert.Single(firstCall);

        // Thêm sản phẩm nổi bật khác thẳng vào DbContext, không qua Service -> không invalidate.
        db.Products.Add(NewProduct("SKU-2", "SP nổi bật 2", category.Id, isFeatured: true));
        await db.SaveChangesAsync();

        var secondCall = await service.GetFeaturedAsync();

        Assert.Single(secondCall);
    }

    [Fact]
    public async Task GetLatestAsync_SecondCallWithinTtl_ServesFromCache_DoesNotSeeDirectDbChange()
    {
        await using var db = CreateDbContext();
        var category = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        db.Products.Add(NewProduct("SKU-1", "SP mới 1", category.Id));
        await db.SaveChangesAsync();

        var cache = CreateRealCache();
        var categoryServiceMock = new Mock<ICategoryService>();
        var service = new ProductService(db, CreateAuditLogMock().Object, categoryServiceMock.Object, cache, CreateConfiguration());

        var firstCall = await service.GetLatestAsync();
        Assert.Single(firstCall);

        db.Products.Add(NewProduct("SKU-2", "SP mới 2", category.Id));
        await db.SaveChangesAsync();

        var secondCall = await service.GetLatestAsync();

        Assert.Single(secondCall);
    }

    [Fact]
    public async Task CreateAsync_ThenGetFeaturedAndLatest_InvalidatesBothCaches_SeesNewProductImmediately()
    {
        await using var db = CreateDbContext();
        var category = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        db.Products.Add(NewProduct("SKU-1", "SP cũ", category.Id, isFeatured: true));
        await db.SaveChangesAsync();

        var cache = CreateRealCache();
        var categoryServiceMock = new Mock<ICategoryService>();
        categoryServiceMock
            .Setup(c => c.GetByIdAsync(category.Id))
            .ReturnsAsync(new Couppa.Api.Models.Responses.CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                IsActive = true,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            });
        var service = new ProductService(db, CreateAuditLogMock().Object, categoryServiceMock.Object, cache, CreateConfiguration());

        // Nạp cả 2 cache trước khi CUD.
        var featuredBefore = await service.GetFeaturedAsync();
        var latestBefore = await service.GetLatestAsync();
        Assert.Single(featuredBefore);
        Assert.Single(latestBefore);

        await service.CreateAsync(new CreateProductRequest
        {
            Sku = "SKU-2",
            Name = "SP mới tạo",
            CategoryId = category.Id,
            Price = 200,
            StockQuantity = 5,
            IsFeatured = true,
            ImageUrls = Array.Empty<string>()
        });

        var featuredAfter = await service.GetFeaturedAsync();
        var latestAfter = await service.GetLatestAsync();

        Assert.Equal(2, featuredAfter.Count);
        Assert.Contains(featuredAfter, p => p.Sku == "SKU-2");
        Assert.Equal(2, latestAfter.Count);
        Assert.Contains(latestAfter, p => p.Sku == "SKU-2");
    }
}
