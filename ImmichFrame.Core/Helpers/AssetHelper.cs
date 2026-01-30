// ImmichFrame.Core/Helpers/AssetHelper.cs
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Helpers;

public static class AssetHelper
{
    public static async Task<IEnumerable<AssetResponseDto>> GetExcludedAlbumAssets(ImmichApi immichApi, IAccountSettings accountSettings, CancellationToken ct = default)
    {
        var excludedAlbumAssets = new List<AssetResponseDto>();

        foreach (var albumId in accountSettings?.ExcludedAlbums ?? new())
        {
            var albumInfo = await immichApi.GetAlbumInfoAsync(albumId, null, null, ct);
            if (albumInfo.Assets != null)
            {
                excludedAlbumAssets.AddRange(albumInfo.Assets);
            }
        }

        return excludedAlbumAssets;
    }
}