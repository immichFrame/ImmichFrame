using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class TagAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var tagAssets = new List<AssetResponseDto>();
        var allTags = await immichApi.GetAllTagsAsync(ct);
        var tagValueToIds = allTags
            .ToDictionary(t => t.Value, t => new Guid(t.Id), StringComparer.OrdinalIgnoreCase);

        // Find the tag IDs for the configured tag values
        var tagIds = new List<Guid>();
        foreach (var tagValue in accountSettings.Tags)
        {
            if (tagValueToIds.TryGetValue(tagValue, out var id))
            {
                tagIds.Add(id);
            }
        }

        var seenIds = new HashSet<string>();
        foreach (var tagId in tagIds)
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
                    TagIds = [tagId],
                    Type = AssetTypeEnum.IMAGE,
                    WithExif = true,
                    WithPeople = true
                };

                var tagInfo = await immichApi.SearchAssetsAsync(metadataBody, ct);

                itemsInPage = tagInfo.Assets.Items.Count;

                // Fetch full asset details to get tag information
                foreach (var asset in tagInfo.Assets.Items)
                {
                    if (seenIds.Contains(asset.Id))
                    {
                        continue;
                    }

                    if (asset.Tags == null)
                    {
                        var assetInfo = await immichApi.GetAssetInfoAsync(new Guid(asset.Id), null, ct);
                        asset.Tags = assetInfo.Tags;
                        asset.ExifInfo = assetInfo.ExifInfo;
                        asset.People = assetInfo.People;
                    }

                    seenIds.Add(asset.Id);
                    tagAssets.Add(asset);
                }

                page++;
            } while (itemsInPage == batchSize);
        }

        return tagAssets;
    }
}
