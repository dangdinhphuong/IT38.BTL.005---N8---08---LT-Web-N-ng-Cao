using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Couppa.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Couppa.Api.Tests.Services;

public class ProductServiceTests
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

    private static ProductService CreateService(
        AppDbContext db,
        out Mock<IAuditLogService> auditLogMock,
        out Mock<ICategoryService> categoryServiceMock,
        long activeCategoryId = 1)
    {
        auditLogMock = new Mock<IAuditLogService>();
        auditLogMock
            .Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        categoryServiceMock = new Mock<ICategoryService>();
        categoryServiceMock
            .Setup(c => c.GetByIdAsync(activeCategoryId))
            .ReturnsAsync(new CategoryResponse
            {
                Id = activeCategoryId,
                Name = "Điện thoại",
                Slug = "dien-thoai",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        // Cache thật (không mock): mỗi test dùng 1 MemoryCache riêng, tránh rò rỉ trạng thái cache giữa các test.
        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        return new ProductService(db, auditLogMock.Object, categoryServiceMock.Object, cache, CreateTestConfiguration());
    }

    private static Category NewCategory(long id, string name, string slug) => new()
    {
        Id = id,
        Name = name,
        Slug = slug,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static Product NewProduct(
        string sku, string name, long categoryId, decimal price = 100, int stock = 10,
        bool isActive = true, bool isDeleted = false, bool isFeatured = false) => new()
    {
        Sku = sku,
        Name = name,
        CategoryId = categoryId,
        Price = price,
        StockQuantity = stock,
        IsActive = isActive,
        IsDeleted = isDeleted,
        IsFeatured = isFeatured,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static CreateProductRequest NewCreateRequest(
        string sku = "SKU-001", string name = "Sản phẩm test", long categoryId = 1,
        decimal price = 100, int stockQuantity = 10) => new()
    {
        Sku = sku,
        Name = name,
        CategoryId = categoryId,
        Price = price,
        StockQuantity = stockQuantity,
        ImageUrls = Array.Empty<string>()
    };

    [Fact]
    public async Task CreateAsync_DuplicateSku_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        db.Products.Add(NewProduct("SKU-001", "SP có sẵn", categoryId: 1));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _, out _);

        var request = NewCreateRequest(sku: "SKU-001", name: "SP mới");

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(request));
        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PRODUCT_SKU_EXISTS", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_NegativePrice_ThrowsUnprocessable()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _, out _);

        var request = NewCreateRequest(price: -1);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(request));
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.Equal("PRODUCT_PRICE_NEGATIVE", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_NegativeStock_ThrowsUnprocessable()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _, out _);

        var request = NewCreateRequest(stockQuantity: -5);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(request));
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.Equal("PRODUCT_STOCK_NEGATIVE", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_CategoryNotFound_PropagatesNotFound()
    {
        await using var db = CreateDbContext();

        var auditLogMock = new Mock<IAuditLogService>();
        var categoryServiceMock = new Mock<ICategoryService>();
        categoryServiceMock
            .Setup(c => c.GetByIdAsync(It.IsAny<long>()))
            .ThrowsAsync(AppException.NotFound("CATEGORY_NOT_FOUND", "Không tìm thấy danh mục."));

        var service = new ProductService(
            db,
            auditLogMock.Object,
            categoryServiceMock.Object,
            new MemoryCacheService(new MemoryCache(new MemoryCacheOptions())),
            CreateTestConfiguration());

        var request = NewCreateRequest(categoryId: 999);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(request));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Equal("CATEGORY_NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndLogsAudit()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        await db.SaveChangesAsync();

        var service = CreateService(db, out var auditLogMock, out _);

        var request = NewCreateRequest();
        var result = await service.CreateAsync(request);

        Assert.Equal("SKU-001", result.Sku);
        Assert.True(result.IsActive);
        Assert.Equal(1, await db.Products.CountAsync());

        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.ProductCreate, EntityType.Product, It.IsAny<long?>(), It.IsAny<object?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPublicListAsync_ExcludesInactiveAndDeleted()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        db.Products.AddRange(
            NewProduct("SKU-ACTIVE", "SP Active", 1, isActive: true, isDeleted: false),
            NewProduct("SKU-INACTIVE", "SP Inactive", 1, isActive: false, isDeleted: false),
            NewProduct("SKU-DELETED", "SP Deleted", 1, isActive: true, isDeleted: true));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _, out _);

        var result = await service.GetPublicListAsync(new ProductListQuery());

        Assert.Single(result.Items);
        Assert.Equal("SKU-ACTIVE", result.Items[0].Sku);
    }

    [Fact]
    public async Task GetAdminListAsync_IncludesInactive_ButExcludesDeleted()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        db.Products.AddRange(
            NewProduct("SKU-ACTIVE", "SP Active", 1, isActive: true, isDeleted: false),
            NewProduct("SKU-INACTIVE", "SP Inactive", 1, isActive: false, isDeleted: false),
            NewProduct("SKU-DELETED", "SP Deleted", 1, isActive: true, isDeleted: true));
        await db.SaveChangesAsync();

        var service = CreateService(db, out _, out _);

        var result = await service.GetAdminListAsync(new AdminProductListQuery());

        Assert.Equal(2, result.TotalItems);
        Assert.Contains(result.Items, p => p.Sku == "SKU-ACTIVE");
        Assert.Contains(result.Items, p => p.Sku == "SKU-INACTIVE");
        Assert.DoesNotContain(result.Items, p => p.Sku == "SKU-DELETED");
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_DoesNotHardDelete()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        var product = NewProduct("SKU-001", "SP test", 1);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db, out var auditLogMock, out _);

        await service.DeleteAsync(product.Id);

        // Global Query Filter loại is_deleted=true khỏi DbSet.Products - phải IgnoreQueryFilters để kiểm tra record vẫn còn trong DB.
        var stillInDb = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == product.Id);
        Assert.NotNull(stillInDb);
        Assert.True(stillInDb!.IsDeleted);

        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.ProductDelete, EntityType.Product, product.Id, It.IsAny<object?>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnSku_DoesNotThrowConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        var product = NewProduct("SKU-001", "SP test", 1, price: 100, stock: 10);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db, out var auditLogMock, out _);

        var request = new UpdateProductRequest
        {
            Sku = "SKU-001", // giữ nguyên SKU của chính nó
            Name = "SP test đã sửa",
            CategoryId = 1,
            Price = 150,
            StockQuantity = 20,
            ImageUrls = Array.Empty<string>()
        };

        var result = await service.UpdateAsync(product.Id, request);

        Assert.Equal("SKU-001", result.Sku);
        Assert.Equal("SP test đã sửa", result.Name);
        Assert.Equal(150, result.Price);

        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.ProductUpdate, EntityType.Product, product.Id, It.IsAny<object?>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SkuUsedByAnotherProduct_ThrowsConflict()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        var productA = NewProduct("SKU-A", "SP A", 1);
        var productB = NewProduct("SKU-B", "SP B", 1);
        db.Products.AddRange(productA, productB);
        await db.SaveChangesAsync();

        var service = CreateService(db, out _, out _);

        var request = new UpdateProductRequest
        {
            Sku = "SKU-A", // trùng với productA
            Name = "SP B đổi tên",
            CategoryId = 1,
            Price = 100,
            StockQuantity = 10,
            ImageUrls = Array.Empty<string>()
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(productB.Id, request));
        Assert.Equal(System.Net.HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("PRODUCT_SKU_EXISTS", ex.Code);
    }

    [Fact]
    public async Task ChangeStatusAsync_UpdatesIsActive_AndLogsAudit()
    {
        await using var db = CreateDbContext();
        db.Categories.Add(NewCategory(1, "Điện thoại", "dien-thoai"));
        var product = NewProduct("SKU-001", "SP test", 1, isActive: true);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var service = CreateService(db, out var auditLogMock, out _);

        var result = await service.ChangeStatusAsync(product.Id, false);

        Assert.False(result.IsActive);

        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.ProductStatusChange, EntityType.Product, product.Id, It.IsAny<object?>()),
            Times.Once);
    }
}
