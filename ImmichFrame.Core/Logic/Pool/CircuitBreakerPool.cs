using ImmichFrame.Core.Api;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool;

public class CircuitBreakerPool(
    IAssetPool primary,
    IAssetPool secondary,
    ILogger<CircuitBreakerPool> logger) :
    BaseCircuitBreakerPool<CircuitBreakerPool>(primary, secondary, logger)
{
}

public abstract class BaseCircuitBreakerPool<T>(
    IAssetPool primary,
    IAssetPool secondary,
    ILogger<T> logger
) : BaseCircuitBreaker(logger), IAssetPool
    where T : BaseCircuitBreakerPool<T>
{
    public async Task<long> GetAssetCount(CancellationToken ct = default)
        => await DoCall(
            () => primary.GetAssetCount(ct),
            () => secondary.GetAssetCount(ct));

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
        => await DoCall(
            () => primary.GetAssets(requested, ct),
            () => secondary.GetAssets(requested, ct));
}

public class BaseCircuitBreaker(ILogger<BaseCircuitBreaker> logger)
{
    private DateTime _brokenUntil = DateTime.MinValue;

    private static readonly TimeSpan BreakerTimeout = TimeSpan.FromDays(7);

    // Made DoCall async to correctly handle exceptions from async primaryFn
    protected async Task<TOut> DoCall<TOut>(Func<Task<TOut>> primaryFnAsync, Func<Task<TOut>> secondaryFnAsync)
    {
        if (!IsBroken)
        {
            try
            {
                return await primaryFnAsync();
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failure when calling primary; breaking circuit and using fallback");
                Break();
            }
        }

        return await secondaryFnAsync();
    }

    // Overload for synchronous functions, if still needed elsewhere, though current usage seems async
    protected TOut DoCall<TOut>(Func<TOut> primaryFn, Func<TOut> secondaryFn)
    {
        if (!IsBroken)
        {
            try
            {
                return primaryFn();
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failure when calling primary; breaking circuit and using fallback");
                Break();
            }
        }
        return secondaryFn();
    }

    public bool IsBroken
    {
        get => _brokenUntil > DateTime.UtcNow;
    }

    public void Break() => _brokenUntil = DateTime.UtcNow.Add(BreakerTimeout);
}