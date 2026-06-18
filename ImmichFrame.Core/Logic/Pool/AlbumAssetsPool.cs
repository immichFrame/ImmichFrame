using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class AlbumAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var albumAssets = new List<AssetResponseDto>();

        var albums = accountSettings.Albums;
        if (albums != null)
        {
            foreach (var albumId in albums)
            {
                var metadataBody = new MetadataSearchDto { AlbumIds = [albumId] };
                var searchResponse = await immichApi.SearchAssetsAsync(metadataBody, ct);
                albumAssets.AddRange(searchResponse.Assets.Items);
            }
        }

        return albumAssets;
    }
}