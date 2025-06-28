using System.Collections;
using BloomFilter;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.AccountSelection;

public class BloomFilterAssetAccountTracker(ILogger<BloomFilterAssetAccountTracker> _logger) : IAssetAccountTracker
{
    private IDictionary<IAccountImmichFrameLogic, IBloomFilter> logicToFilter = new Dictionary<IAccountImmichFrameLogic, IBloomFilter>();

    public async ValueTask<bool> RecordAssetLocation(IAccountImmichFrameLogic account, string assetId)
    {
        var filter = await logicToFilter.GetOrCreateAsync(account, NewFilter);
        return await filter.AddAsync(assetId);
    }

    private async Task<IBloomFilter> NewFilter(IImmichFrameLogic account)
    {
        return FilterBuilder.Build(await account.GetTotalAssets());
    }

    public T ForAsset<T>(string assetId, Func<IAccountImmichFrameLogic, T> f)
    {
        foreach (var entry in logicToFilter)
        {
            if (entry.Value.Contains(assetId))
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