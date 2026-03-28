using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class TagAssetsPool : CachingApiAssetsPool
{
    public TagAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings)
        : base(apiCache, immichApi, accountSettings)
    {
    }

    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var tagAssets = new List<AssetResponseDto>();

        if (AccountSettings.Tags == null)
        {
            return tagAssets;
        }

        var allTags = await ApiCache.GetOrAddAsync(
            $"allTags_{AccountSettings.ImmichServerUrl}",
            () => ImmichApi.GetAllTagsAsync(ct));
        var tagValueToTag = allTags.ToDictionary(t => t.Value);

        // Find the tags for the configured tag values
        var tags = new List<TagResponseDto>();
        foreach (var tagValue in AccountSettings.Tags)
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

                if (!AccountSettings.ShowVideos)
                {
                    metadataBody.Type = AssetTypeEnum.IMAGE;
                }

                var tagInfo = await ImmichApi.SearchAssetsAsync(metadataBody, ct);

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
