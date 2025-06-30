using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.Core.Helpers;

public class ApiCache : IApiCache, IDisposable
{
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public ApiCache(TimeSpan cacheDuration)
    {
        _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = cacheDuration
        };
    }

    public virtual Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
        => _cache.GetOrCreateAsync<T>(key, _ => factory(), _cacheOptions);

    public void Dispose()
        => _cache.Dispose();
}