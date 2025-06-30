using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool.Preload;

public class AlbumAssetsPreloadPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : PreloadedAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var excludedAlbumAssets = new List<AssetResponseDto>();

        foreach (var albumId in accountSettings.ExcludedAlbums)
        {
            var albumInfo = await immichApi.GetAlbumInfoAsync(albumId, null, null, ct);
            excludedAlbumAssets.AddRange(albumInfo.Assets);
        }

        var albumAssets = new List<AssetResponseDto>();

        foreach (var albumId in accountSettings.Albums)
        {
            var albumInfo = await immichApi.GetAlbumInfoAsync(albumId, null, null, ct);
            albumAssets.AddRange(albumInfo.Assets);
        }

        return albumAssets.WhereExcludes(excludedAlbumAssets, t => t.Id);
    }
}