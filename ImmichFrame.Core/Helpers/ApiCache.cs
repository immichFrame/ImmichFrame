public class ApiCache<T> : IDisposable
{
    private readonly TimeSpan _cacheDuration;
    private readonly Dictionary<string, (DateTime Timestamp, T Data)> _cache = new();

    public ApiCache(TimeSpan cacheDuration)
    {
        _cacheDuration = cacheDuration;
    }

    public async Task<T> GetOrAddAsync(string key, Func<Task<T>> factory)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow - entry.Timestamp < _cacheDuration)
            {
                return entry.Data;
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
