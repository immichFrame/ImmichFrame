using Microsoft.Extensions.Logging;
namespace ImmichFrame.Core.Tests.Logic.Pool;

public static class FixtureHelpers
{
    public static ILogger<T> TestLogger<T>()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        return loggerFactory.CreateLogger<T>();
    }

    public class ForgetfulCountingCache : IApiCache
    {
        public int Count = 0;
        public Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
        {
            Count++;
            return factory();
        }
    }
}