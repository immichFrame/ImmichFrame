public class ApiCache : IDisposable
{
    private readonly TimeSpan _cacheDuration;
    private readonly Dictionary<string, (DateTime Timestamp, object Data)> _cache = new();

    public ApiCache(TimeSpan cacheDuration)
    {
        _cacheDuration = cacheDuration;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow - entry.Timestamp < _cacheDuration)
            {
                return (T)entry.Data;
            }

            Invalidate(key); // Cache expired
        }

        return default;
    }

    public virtual async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
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

    public void Invalidate(string key)
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