using System.Collections.Concurrent;
using BloomFilter;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.AccountSelection;

public class BloomFilterAssetAccountTracker(ILogger<BloomFilterAssetAccountTracker> _logger) : IAssetAccountTracker
{
    // Lazy<Task<...>> so concurrent RecordAssetLocation calls for the same account build
    // exactly one filter (GetOrAdd alone doesn't guarantee single execution of the factory).
    private readonly ConcurrentDictionary<IAccountImmichFrameLogic, Lazy<Task<IBloomFilter>>> logicToFilter = new();

    public async ValueTask<bool> RecordAssetLocation(IAccountImmichFrameLogic account, Guid assetId)
    {
        var lazyFilter = logicToFilter.GetOrAdd(account,
            acc => new Lazy<Task<IBloomFilter>>(() => NewFilter(acc)));
        IBloomFilter filter;
        try
        {
            filter = await lazyFilter.Value;
        }
        catch
        {
            // A faulted Lazy would otherwise be cached forever; evict it (only if it is
            // still this exact entry) so the next call rebuilds the filter.
            logicToFilter.TryRemove(KeyValuePair.Create(account, lazyFilter));
            throw;
        }
        return await filter.AddAsync(assetId.ToString());
    }

    private async Task<IBloomFilter> NewFilter(IImmichFrameLogic account)
    {
        return FilterBuilder.Build(await account.GetTotalAssets());
    }

    public T ForAsset<T>(Guid assetId, Func<IAccountImmichFrameLogic, T> f)
    {
        foreach (var entry in logicToFilter)
        {
            var filterTask = entry.Value.Value;
            // A filter still being built cannot have recorded this asset yet.
            if (!filterTask.IsCompletedSuccessfully) continue;

            if (filterTask.Result.Contains(assetId.ToString()))
            {
                try
                {
                    return f(entry.Key);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to locate asset {assetId} in {entry.Key}. Must be false positive, trying next account.", assetId, entry.Key);
                }
            }
        }

        _logger.LogError("Failed to locate account for asset {assetId}", assetId);
        throw new AssetNotFoundException();
    }
}