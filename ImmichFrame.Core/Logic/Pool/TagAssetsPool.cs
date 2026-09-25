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

        if (tags.Count == 0)
        {
            return tagAssets;
        }

        var types = new List<AssetTypeEnum> { AssetTypeEnum.IMAGE };
        if (accountSettings.ShowVideos)
        {
            types.Add(AssetTypeEnum.VIDEO);
        }

        // Search does not return tags. Query each configured tag so the matching tag can be attached.
        var seen = new Dictionary<Guid, AssetResponseDto>();
        const int batchSize = 1000;

        foreach (var tag in tags)
        {
            string? nextCursor = null;
            do
            {
                var metadataBody = new MetadataSearchDto
                {
                    Size = batchSize,
                    Cursor = nextCursor,
                    WithExif = true,
                    WithPeople = true,
                    Filter = new SearchFilter
                    {
                        TrashedAt = new NullableDateFilter { Eq = null },
                        TagIds = new IdsFilter { Any = [tag.Id] },
                        Type = new EnumFilterAssetType { In = types }
                    }
                };

                var page = await immichApi.SearchAssetsAsync(null, null, metadataBody, ct);
                foreach (var asset in page.Assets.Items)
                {
                    if (seen.TryGetValue(asset.Id, out var existing))
                    {
                        existing.Tags!.Add(tag);
                        continue;
                    }

                    asset.Tags = new List<TagResponseDto> { tag };
                    seen[asset.Id] = asset;
                    tagAssets.Add(asset);
                }

                nextCursor = page.Assets.NextCursor;
            } while (nextCursor != null);
        }

        return tagAssets;
    }
}
