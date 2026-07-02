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
            int page = 1;
            int batchSize = 1000;
            int itemsInPage;
            do
            {
                var metadataBody = new MetadataSearchDto
                {
                    Page = page,
                    Size = batchSize,
                    AlbumIds = [albumId]
                };
                var searchResponse = await immichApi.SearchAssetsAsync(metadataBody, ct);

                itemsInPage = searchResponse.Assets?.Items.Count ?? 0;

                if (searchResponse.Assets != null)
                {
                    excludedAlbumAssets.AddRange(searchResponse.Assets.Items);
                }

                page++;
            } while (itemsInPage == batchSize);
        }

        return excludedAlbumAssets;
    }
}