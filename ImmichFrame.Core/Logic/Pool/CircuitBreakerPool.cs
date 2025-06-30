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
    public Task<long> GetAssetCount(CancellationToken ct = default)
        => DoCall(
            () => primary.GetAssetCount(ct),
            () => secondary.GetAssetCount(ct));

    public Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
        => DoCall(
            () => primary.GetAssets(requested, ct),
            () => secondary.GetAssets(requested, ct));
}

public class BaseCircuitBreaker(ILogger<BaseCircuitBreaker> logger)
{
    private DateTime _brokenUntil = DateTime.MinValue;

    private static readonly TimeSpan BreakerTimeout = TimeSpan.FromDays(7);

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