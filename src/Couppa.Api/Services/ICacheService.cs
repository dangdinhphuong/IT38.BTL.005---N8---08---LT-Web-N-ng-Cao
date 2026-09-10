namespace Couppa.Api.Services;

/// <summary>
/// Bọc IMemoryCache (SRS Chương 11). GetOrCreateAsync dùng cho đọc, Remove dùng cho invalidation.
/// KHÔNG dùng cho dữ liệu cá nhân hóa (giỏ hàng, profile) — cache in-process dùng chung mọi người dùng.
/// </summary>
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory);
    void Remove(string key);
}
