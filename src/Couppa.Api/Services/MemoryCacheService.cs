using Microsoft.Extensions.Caching.Memory;

namespace Couppa.Api.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory();
        _cache.Set(key, value, ttl);
        return value;
    }

    public void Remove(string key) => _cache.Remove(key);
}

/// <summary>Cache key cố định dùng xuyên suốt hệ thống (SRS Chương 11).</summary>
public static class CacheKeys
{
    public const string CategoriesActive = "categories:active";
    public const string ProductsFeatured = "products:featured";
    public const string ProductsLatest = "products:latest";
    public const string DashboardSummary = "admin:dashboard:summary";
}
