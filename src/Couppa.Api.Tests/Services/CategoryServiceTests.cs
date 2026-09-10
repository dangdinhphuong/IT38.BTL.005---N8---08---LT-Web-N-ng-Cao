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

public class CategoryServiceTests
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

    private static CategoryService CreateService(AppDbContext db, out Mock<IAuditLogService> auditLogMock)
    {
        auditLogMock = new Mock<IAuditLogService>();
        auditLogMock
            .Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);
        // Cache thật (không mock): mỗi test dùng 1 MemoryCache riêng, tránh rò rỉ trạng thái cache giữa các test.
        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        return new CategoryService(db, auditLogMock.Object, cache, CreateTestConfiguration());
    }

    private static Category NewCategory(string name, string slug, bool isActive = true) => new()
    {
        Name = name,
        Slug = slug,
        IsActive = isActive,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory("Điện thoại", "dien-thoai"));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _);

        var request = new CreateCategoryRequest { Name = "Điện thoại", Slug = "dien-thoai-2" };

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(request));
        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("CATEGORY_NAME_EXISTS", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory("Điện thoại", "dien-thoai"));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _);

        var request = new CreateCategoryRequest { Name = "Điện thoại 2", Slug = "dien-thoai" };

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(request));
        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("CATEGORY_SLUG_EXISTS", ex.Code);
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithProducts_ThrowsConflictWithProductCount()
    {
        await using var db = CreateDbContext();
        var category = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product
            {
                Sku = "SKU-1",
                Name = "SP 1",
                CategoryId = category.Id,
                Price = 100,
                StockQuantity = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new Product
            {
                Sku = "SKU-2",
                Name = "SP 2",
                CategoryId = category.Id,
                Price = 200,
                StockQuantity = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        await db.SaveChangesAsync();

        var service = CreateService(db, out _);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.DeleteAsync(category.Id));

        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("CATEGORY_HAS_PRODUCTS", ex.Code);

        var productCount = (int)ex.Details!.GetType().GetProperty("productCount")!.GetValue(ex.Details)!;
        Assert.Equal(2, productCount);

        Assert.Equal(1, await db.Categories.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithoutProducts_DeletesSuccessfully()
    {
        await using var db = CreateDbContext();
        var category = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var service = CreateService(db, out var auditLogMock);

        await service.DeleteAsync(category.Id);

        Assert.Equal(0, await db.Categories.CountAsync());
        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.CategoryDelete, EntityType.Category, category.Id, It.IsAny<object?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveListAsync_ExcludesInactive_ButGetAllForAdminAsync_IncludesIt()
    {
        await using var db = CreateDbContext();
        db.Categories.AddRange(
            NewCategory("Điện thoại", "dien-thoai", isActive: true),
            NewCategory("Phụ kiện", "phu-kien", isActive: false));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _);

        var activeList = await service.GetActiveListAsync();
        Assert.Single(activeList);
        Assert.Equal("dien-thoai", activeList[0].Slug);

        var adminList = await service.GetAllForAdminAsync(search: null, page: 1, pageSize: 20);
        Assert.Equal(2, adminList.TotalItems);
        Assert.Contains(adminList.Items, c => c.Slug == "phu-kien");
        Assert.Contains(adminList.Items, c => c.Slug == "dien-thoai");
    }

    [Fact]
    public async Task ChangeStatusAsync_UpdatesIsActive()
    {
        await using var db = CreateDbContext();
        var category = NewCategory("Điện thoại", "dien-thoai", isActive: true);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var service = CreateService(db, out var auditLogMock);

        var result = await service.ChangeStatusAsync(category.Id, false);

        Assert.False(result.IsActive);
        var reloaded = await db.Categories.FindAsync(category.Id);
        Assert.False(reloaded!.IsActive);

        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.CategoryStatusChange, EntityType.Category, category.Id, It.IsAny<object?>()),
            Times.Once);
    }
}
