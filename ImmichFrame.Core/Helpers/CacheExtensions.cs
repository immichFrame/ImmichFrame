using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.Core.Helpers;


static class CacheExtensions
{
    public static async Task<T> GetOrAddAsync<T>(this IMemoryCache cache, string key, TimeSpan absoluteExpiry, Func<Task<T>> factory)
    {
        var rv = cache.Get<T>(key);

        if (rv == null)
        {
            rv = await factory();
            cache.Set(key, rv, absoluteExpiry);
        }

        return rv;
    }
}

public class ApiCache1 : IDisposable
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public ApiCache1(TimeSpan cacheDuration)
    {
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(cacheDuration);
    }

    public T? GetAsync<T>(string key) => _cache.Get<T>(key);

    public virtual async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
    {
        var rv = GetAsync<T>(key);

        if (rv == null)
        {
            rv = await factory();
            _cache.Set(key, rv);
        }

        return rv;
    }

    public T Add<T>(string key, T value) => _cache.Set(key, value, _cacheOptions);

    public void Dispose()
    {
        _cache.Dispose();
    }
}