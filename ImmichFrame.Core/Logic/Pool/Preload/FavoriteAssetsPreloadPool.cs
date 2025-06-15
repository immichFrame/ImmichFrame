using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool.Preload;

public class FavoriteAssetsPreloadPool(ApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : PreloadedAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
        => LoadAssetsFromMetadataSearch(new MetadataSearchDto
        {
            IsFavorite = true,
            WithExif = true,
            WithPeople = true
        }, ct);
}