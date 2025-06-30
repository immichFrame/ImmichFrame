using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;

namespace ImmichFrame.Core.Logic.Pool;

public class MultiAssetPool(IEnumerable<IAssetPool> delegates) : AggregatingAssetPool
{
    public override async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        var counts = delegates.Select(pool => pool.GetAssetCount(ct));
        return (await Task.WhenAll(counts)).Sum();
    }

    protected override async Task<AssetResponseDto?> GetNextAsset(CancellationToken ct)
    {
        if (!delegates.Any())
        {
            return null;
        }

        var assetCounts = await Task.WhenAll(delegates.Select(async d => await d.GetAssetCount(ct)));
        if (assetCounts.Sum() == 0)
        {
            return null;
        }

        var pool = await delegates.ChooseOne(async @delegate => await @delegate.GetAssetCount(ct));
        // It's possible that the chosen pool.GetAssetCount(ct) returns 0 if it was just exhausted
        // by another concurrent request. Or if ChooseOne selects a pool that has 0 assets
        // while other pools still have assets (e.g. if random index falls into its zero range).
        // The original ChooseOne logic could throw if totalCount is 0.
        // The new checks above should prevent ChooseOne from being called with totalCount = 0.
        // However, the selected 'pool' itself might still have 0 assets.
        // In that case, pool.GetAssets(1, ct) should return an empty list, and FirstOrDefault() will be null.
        return (await pool.GetAssets(1, ct)).FirstOrDefault();
    }
}