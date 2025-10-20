using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class FavoriteAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var favoriteAssets = new List<AssetResponseDto>();

        int page = 1;
        int batchSize = 1000;
        int total;
        do
        {
            var metadataBody = new MetadataSearchDto
            {
                Page = page,
                Size = batchSize,
                IsFavorite = true,
                WithExif = true,
                WithPeople = true
            };

            if (!accountSettings.ShowVideos)
            {
                metadataBody.Type = AssetTypeEnum.IMAGE;
            }

            var favoriteInfo = await immichApi.SearchAssetsAsync(metadataBody, ct);

            total = favoriteInfo.Assets.Total;

            favoriteAssets.AddRange(favoriteInfo.Assets.Items);
            page++;
        } while (total == batchSize);

        return favoriteAssets;
    }
}