using ImmichFrame.Core.Api;

namespace ImmichFrame.Core.Logic.Pool;

public abstract class AggregatingAssetPool : IAssetPool
{
    public abstract Task<long> GetAssetCount(CancellationToken ct = default);
    protected abstract Task<AssetResponseDto?> GetNextAsset(string clientId, CancellationToken ct);

    public virtual Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, string clientId = "", CancellationToken ct = default)
    {
        return IAssetPool.WaitAssets(requested, token => GetNextAsset(clientId, token), ct);
    }
}