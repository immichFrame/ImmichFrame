using ImmichFrame.Core.Api;

namespace ImmichFrame.Core.Logic.Pool;

public abstract class AggregatingAssetPool : IAssetPool
{
    public abstract Task<long> GetAssetCount(CancellationToken ct = default);
    protected abstract Task<AssetResponseDto?> GetNextAsset(CancellationToken ct);

    public Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        return IAssetPool.WaitAssets(requested, GetNextAsset, ct);
    }
}