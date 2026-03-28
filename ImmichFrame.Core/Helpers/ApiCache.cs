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

    public virtual async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory) where T : notnull
    {
        var value = await _cache.GetOrCreateAsync<T>(key, _ => factory(), _cacheOptions());
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }

    public void Dispose()
        => _cache.Dispose();
}
