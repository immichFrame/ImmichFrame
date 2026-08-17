using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class AlbumAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var albumAssets = new List<AssetResponseDto>();

        var albums = await GetAlbumIds(ct);
        if (albums != null)
        {
            foreach (var albumId in albums)
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
                        AlbumIds = [albumId],
                        WithExif = true,
                        WithPeople = true,
                    };
                    var searchResponse = await immichApi.SearchAssetsAsync(null, null, metadataBody, ct);

                    itemsInPage = searchResponse.Assets.Items.Count;

                    albumAssets.AddRange(searchResponse.Assets.Items);
                    page++;
                } while (itemsInPage == batchSize);
            }
        }

        return albumAssets.DistinctBy(asset => asset.Id);
    }

    private async Task<IEnumerable<Guid>?> GetAlbumIds(CancellationToken ct)
    {
        if (!accountSettings.ShowOnlyAssetsInAlbums)
        {
            return accountSettings.Albums;
        }

        var albums = await immichApi.GetAllAlbumsAsync(null, null, null, null, null, ct);
        return albums.Select(album => album.Id);
    }
}
