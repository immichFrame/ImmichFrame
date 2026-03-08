using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class MultiAssetPool(IEnumerable<IAssetPool> delegates) : AggregatingAssetPool
{
    public override async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        var counts = delegates.Select(pool => pool.GetAssetCount(ct));
        return (await Task.WhenAll(counts)).Sum();
    }

    protected override async Task<AssetResponseDto?> GetNextAsset(IRequestContext requestContext, CancellationToken ct)
    {
        var pool = await delegates.ChooseOne(async @delegate => await @delegate.GetAssetCount(ct));

        if (pool == null) return null;

        return (await pool.GetAssets(1, requestContext, ct)).FirstOrDefault();
    }
}
