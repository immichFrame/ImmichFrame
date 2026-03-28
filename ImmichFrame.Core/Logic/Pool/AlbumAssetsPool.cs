using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class AlbumAssetsPool : CachingApiAssetsPool
{
    public AlbumAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings)
        : base(apiCache, immichApi, accountSettings)
    {
    }

    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var albumAssets = new List<AssetResponseDto>();

        var albums = AccountSettings.Albums;
        if (albums != null)
        {
            foreach (var albumId in albums)
            {
                var albumInfo = await ImmichApi.GetAlbumInfoAsync(albumId, null, null, ct);
                albumAssets.AddRange(albumInfo.Assets);
            }
        }

        return albumAssets;
    }
}
