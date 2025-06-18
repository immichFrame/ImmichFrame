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
        var pool = await delegates.ChooseOne(async @delegate=> await @delegate.GetAssetCount(ct));
        
        if (pool == null) return null;
        
        return (await pool.GetAssets(1, ct)).FirstOrDefault();
    }
}