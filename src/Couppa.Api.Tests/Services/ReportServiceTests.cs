using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Couppa.Api.Tests.Services;

public class ReportServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ReportService CreateService(AppDbContext db) => new(db);

    private static Category NewCategory(string name, string slug) => new()
    {
        Name = name,
        Slug = slug,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static Product NewProduct(string sku, string name, long categoryId) => new()
    {
        Sku = sku,
        Name = name,
        CategoryId = categoryId,
        Price = 100,
        StockQuantity = 10,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static CartActivityLog NewLog(long productId, string action, DateTimeOffset createdAt) => new()
    {
        ProductId = productId,
        Action = action,
        Quantity = 1,
        CreatedAt = createdAt
    };

    [Fact]
    public async Task GetCartTopProductsAsync_EmptyLogs_ReturnsEmptyList_DoesNotThrow()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetCartTopProductsAsync(from: null, to: null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCartTopProductsAsync_OrdersByAddCountDescending_LimitsToTop10()
    {
        await using var db = CreateDbContext();
        var cat = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        // 12 sản phẩm, mỗi sản phẩm có (index+1) lần 'add' -> SP có index cao nhất phải đứng đầu.
        var products = new List<Product>();
        for (var i = 1; i <= 12; i++)
        {
            var product = NewProduct($"SKU-{i}", $"SP {i}", cat.Id);
            products.Add(product);
        }
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < products.Count; i++)
        {
            var addCount = i + 1;
            for (var j = 0; j < addCount; j++)
            {
                db.CartActivityLogs.Add(NewLog(products[i].Id, CartActivityAction.Add, now.AddMinutes(-j)));
            }
        }
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetCartTopProductsAsync(from: null, to: null);

        Assert.Equal(10, result.Count);
        // SP 12 có 12 lần add -> nhiều nhất -> đứng đầu.
        Assert.Equal("SP 12", result[0].ProductName);
        Assert.Equal(12, result[0].AddCount);
        // SP 3 có 3 lần add -> đứng thứ 10 (loại SP 1, SP 2 vì ít nhất).
        Assert.Equal("SP 3", result[9].ProductName);
        Assert.DoesNotContain(result, r => r.ProductName is "SP 1" or "SP 2");
    }

    [Fact]
    public async Task GetCartTopProductsAsync_ExcludesRemoveAction_CountsOnlyAdd()
    {
        await using var db = CreateDbContext();
        var cat = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(cat);
        var product = NewProduct("SKU-1", "SP 1", cat.Id);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        db.CartActivityLogs.AddRange(
            NewLog(product.Id, CartActivityAction.Add, now),
            NewLog(product.Id, CartActivityAction.Add, now),
            NewLog(product.Id, CartActivityAction.Remove, now));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetCartTopProductsAsync(from: null, to: null);

        Assert.Single(result);
        Assert.Equal(2, result[0].AddCount);
    }

    [Fact]
    public async Task GetCartTopProductsAsync_FiltersByFromToRange()
    {
        await using var db = CreateDbContext();
        var cat = NewCategory("Điện thoại", "dien-thoai");
        db.Categories.Add(cat);
        var product = NewProduct("SKU-1", "SP 1", cat.Id);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        db.CartActivityLogs.AddRange(
            NewLog(product.Id, CartActivityAction.Add, now.AddDays(-40)), // ngoài range mặc định 30 ngày
            NewLog(product.Id, CartActivityAction.Add, now.AddDays(-5)),
            NewLog(product.Id, CartActivityAction.Add, now.AddDays(-1)));
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Không truyền from/to -> mặc định 30 ngày gần nhất -> chỉ đếm 2 log trong range.
        var defaultRange = await service.GetCartTopProductsAsync(from: null, to: null);
        Assert.Single(defaultRange);
        Assert.Equal(2, defaultRange[0].AddCount);

        // Truyền from/to bao trọn cả 3 log -> đếm đủ 3.
        var fullRange = await service.GetCartTopProductsAsync(from: now.AddDays(-60), to: now);
        Assert.Single(fullRange);
        Assert.Equal(3, fullRange[0].AddCount);
    }
}
