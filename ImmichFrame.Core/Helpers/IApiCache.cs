public interface IApiCache
{
    Task<T> GetOrAddAsync<T>(object key, Func<Task<T>> factory);
}

public class ApiCache : IApiCache, IDisposable
{
    private readonly TimeSpan _cacheDuration;
    private readonly Dictionary<object, (DateTime Timestamp, object Data)> _cache = new();

    public ApiCache(TimeSpan cacheDuration)
    {
        _cacheDuration = cacheDuration;
    }

    public async Task<T> GetOrAddAsync<T>(object key, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow - entry.Timestamp < _cacheDuration)
            {
                return (T)entry.Data;
            }
            else
            {
                Invalidate(key); // Cache expired
            }
        }

        // Value is not in cache or expired -> reload
        var data = await factory();
        _cache[key] = (DateTime.UtcNow, data);
        return data;
    }

    public void Invalidate(object key)
    {
        _cache.Remove(key);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public void Dispose()
    {
        Clear();
    }
}