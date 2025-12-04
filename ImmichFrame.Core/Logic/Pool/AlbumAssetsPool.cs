using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class AlbumAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var albumAssets = new List<AssetResponseDto>();

        foreach (var albumId in accountSettings.Albums)
        {
            var albumInfo = await immichApi.GetAlbumInfoAsync(albumId, null, null, ct);
            albumAssets.AddRange(albumInfo.Assets);
        }

        return albumAssets;
    }
}