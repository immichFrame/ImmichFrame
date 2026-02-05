using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public abstract class CachingApiAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : IAssetPool
{
    private readonly Random _random = new();

    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        return (await AllAssets(ct)).Count();
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, IRequestContext requestContext, CancellationToken ct = default)
    {
        var allAssets = await AllAssets(ct);
        var totalCount = allAssets.Count();

        if (requestContext.AssetOffset >= totalCount)
        {
            requestContext.AssetOffset = 0;
        }

        var assetsToReturn = allAssets.Skip(requestContext.AssetOffset).Take(requested);

        requestContext.AssetOffset += assetsToReturn.Count();
        if (requestContext.AssetOffset >= totalCount)
        {
            requestContext.AssetOffset = 0;
        }

        return assetsToReturn;
    }

    private async Task<IEnumerable<AssetResponseDto>> AllAssets(CancellationToken ct = default)
    {
        var excludedAlbumAssets = await apiCache.GetOrAddAsync($"{GetType().FullName}_ExcludedAlbums", () => AssetHelper.GetExcludedAlbumAssets(immichApi, accountSettings));

        return await apiCache.GetOrAddAsync(GetType().FullName!, () => LoadAssets().ApplyAccountFilters(accountSettings, excludedAlbumAssets));
    }

    protected abstract Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default);
}
