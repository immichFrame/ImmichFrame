using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class TagAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var tagAssets = new List<AssetResponseDto>();

        if (accountSettings.Tags == null)
        {
            return tagAssets;
        }

        var allTags = await apiCache.GetOrAddAsync($"allTags_{accountSettings.ImmichServerUrl}",
            () => immichApi.GetAllTagsAsync(ct));
        var tagValueToTag = allTags.ToDictionary(t => t.Value);

        // Find the tags for the configured tag values
        var tags = new List<TagResponseDto>();
        foreach (var tagValue in accountSettings.Tags)
        {
            if (tagValueToTag.TryGetValue(tagValue, out var tag))
            {
                tags.Add(tag);
            }
        }

        var seenIds = new HashSet<string>();
        foreach (var tag in tags)
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
                    TagIds = [new Guid(tag.Id)],
                    WithExif = true,
                    WithPeople = true
                };

                if (!accountSettings.ShowVideos)
                {
                    metadataBody.Type = AssetTypeEnum.IMAGE;
                }

                var tagInfo = await immichApi.SearchAssetsAsync(metadataBody, ct);

                itemsInPage = tagInfo.Assets.Items.Count;

                // Attach the tag that matched this search
                foreach (var asset in tagInfo.Assets.Items)
                {
                    if (seenIds.Contains(asset.Id))
                    {
                        tagAssets.First(a => a.Id == asset.Id).Tags.Add(tag);
                        continue;
                    }

                    // SearchAssetsAsync does not support a `WithTags`
                    // parameter, so simply set the one that was configured
                    asset.Tags = new List<TagResponseDto> { tag };

                    seenIds.Add(asset.Id);
                    tagAssets.Add(asset);
                }

                page++;
            } while (itemsInPage == batchSize);
        }

        return tagAssets;
    }
}
