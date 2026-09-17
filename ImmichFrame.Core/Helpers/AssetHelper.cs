// ImmichFrame.Core/Helpers/AssetHelper.cs
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Helpers;

public static class AssetHelper
{
    public static async Task<IEnumerable<AssetResponseDto>> GetExcludedAlbumAssets(ImmichApi immichApi, IAccountSettings accountSettings, CancellationToken ct = default)
    {
        var excludedAlbumAssets = new List<AssetResponseDto>();

        foreach (var albumId in await GetExcludedAlbumIds(immichApi, accountSettings, ct))
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
                var searchResponse = await immichApi.SearchAssetsAsync(null, null, metadataBody, ct);

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

    /// <summary>
    /// The configured <see cref="IAccountSettings.ExcludedAlbums"/> plus, when
    /// <see cref="IAccountSettings.HideAssetsInOtherAlbums"/> is set, every album (owned or shared)
    /// that is not one of the selected <see cref="IAccountSettings.Albums"/>.
    /// </summary>
    private static async Task<IEnumerable<Guid>> GetExcludedAlbumIds(ImmichApi immichApi, IAccountSettings accountSettings, CancellationToken ct)
    {
        var excludedAlbumIds = accountSettings?.ExcludedAlbums ?? new();

        // Without selected albums every album would count as "other", hiding every album asset
        if (accountSettings?.HideAssetsInOtherAlbums != true || !(accountSettings.Albums?.Count > 0))
        {
            return excludedAlbumIds;
        }

        var selectedAlbumIds = accountSettings.Albums.ToHashSet();
        var allAlbums = await immichApi.GetAllAlbumsAsync(null, null, null, null, null, ct);

        return excludedAlbumIds
            .Concat(allAlbums.Select(album => album.Id).Where(id => !selectedAlbumIds.Contains(id)))
            .Distinct();
    }
}