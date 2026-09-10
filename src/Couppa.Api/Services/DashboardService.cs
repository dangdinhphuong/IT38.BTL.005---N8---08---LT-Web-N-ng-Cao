using Couppa.Api.Data;
using Couppa.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IConfiguration _configuration;

    public DashboardService(AppDbContext db, ICacheService cache, IConfiguration configuration)
    {
        _db = db;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync()
    {
        var ttlMinutes = _configuration.GetValue<double>("Cache:DashboardSummaryTtlMinutes");
        return await _cache.GetOrCreateAsync(CacheKeys.DashboardSummary, TimeSpan.FromMinutes(ttlMinutes), BuildSummaryAsync);
    }

    private async Task<DashboardSummaryResponse> BuildSummaryAsync()
    {
        // Product entity có Global Query Filter (!IsDeleted) áp dụng tự động cho mọi truy vấn dưới đây.
        var totalProducts = await _db.Products.CountAsync();
        var totalCategories = await _db.Categories.CountAsync();
        var totalUsers = await _db.Users.CountAsync();
        var activeProducts = await _db.Products.CountAsync(p => p.IsActive);
        var outOfStockProducts = await _db.Products.CountAsync(p => p.StockQuantity == 0);

        var productsByCategory = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new ProductsByCategoryItem
            {
                CategoryName = c.Name,
                Count = c.Products.Count
            })
            .ToListAsync();

        // FR-DASH-004: top 5 sản phẩm mới thêm gần đây (đã chốt 5, không phải 5-10).
        var recentProducts = await _db.Products
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentProductItem
            {
                Id = p.Id,
                Name = p.Name,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        // DB rỗng: mọi Count* tự nhiên là 0, mọi list tự nhiên rỗng - không throw (task 10, Exception Flow).
        return new DashboardSummaryResponse
        {
            TotalProducts = totalProducts,
            TotalCategories = totalCategories,
            TotalUsers = totalUsers,
            ActiveProducts = activeProducts,
            OutOfStockProducts = outOfStockProducts,
            ProductsByCategory = productsByCategory,
            RecentProducts = recentProducts
        };
    }
}
