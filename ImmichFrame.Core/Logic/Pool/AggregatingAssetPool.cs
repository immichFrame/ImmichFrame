using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;


namespace ImmichFrame.Core.Logic.Pool;

public abstract class AggregatingAssetPool : IAssetPool
{
    public abstract Task<long> GetAssetCount(CancellationToken ct = default);
    protected abstract Task<AssetResponseDto?> GetNextAsset(IRequestContext requestContext, CancellationToken ct);

    public Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, IRequestContext requestContext, CancellationToken ct = default)
    {
        return IAssetPool.WaitAssets(requested, GetNextAsset, requestContext, ct);
    }
}
