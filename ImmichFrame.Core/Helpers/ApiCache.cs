using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.Core.Helpers;

public class ApiCache : IApiCache, IDisposable
{
    private readonly Func<MemoryCacheEntryOptions> _cacheOptions;
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public ApiCache(TimeSpan cacheDuration) : this(() => new MemoryCacheEntryOptions()
    {
        AbsoluteExpirationRelativeToNow = cacheDuration
    })
    {
    }

    public ApiCache(Func<MemoryCacheEntryOptions> entryOptions)
    {
        _cacheOptions = entryOptions;
    }

    public virtual Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
        => _cache.GetOrCreateAsync<T>(key, _ => factory(), _cacheOptions());

    public void Dispose()
        => _cache.Dispose();
}