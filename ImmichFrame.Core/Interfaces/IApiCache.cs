public interface IApiCache
{
    Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory) where T : notnull;
}
