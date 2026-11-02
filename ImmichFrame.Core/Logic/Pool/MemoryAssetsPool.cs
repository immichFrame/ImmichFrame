using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.Core.Logic.Pool;

public class MemoryAssetsPool(ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(new DailyApiCache())
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var searchDate = DateTimeOffset.Now;
        var memories = new List<MemoryResponseDto>();

        // One request per enabled type, so the server filters out any memory type this client
        // does not know. An unknown type in the response fails deserialization of the whole list.
        foreach (var type in EnabledMemoryTypes())
        {
            memories.AddRange(await immichApi.SearchMemoriesAsync(searchDate, null, null, null, null, null, null, null, type, ct));
        }

        var memoryAssets = new List<AssetResponseDto>();
        foreach (var memory in memories)
        {
            var assets = memory.Assets.ToList();

            if (!accountSettings.ShowVideos)
            {
                assets = assets.Where(a => a.Type == AssetTypeEnum.IMAGE).ToList();
            }

            foreach (var asset in assets)
            {
                if (asset.ExifInfo == null)
                {
                    var assetInfo = await immichApi.GetAssetInfoAsync(asset.Id, null, null, ct);
                    asset.ExifInfo = assetInfo.ExifInfo;
                    asset.People = assetInfo.People;
                }

                // A birthday memory's Data.Year is the person's birth year, not the year of the photo.
                var photoYear = memory.Type == MemoryType.Birthday ? asset.LocalDateTime.Year : memory.Data.Year;
                var yearsAgo = searchDate.Year - photoYear;
                asset.ExifInfo.Description = $"{yearsAgo} {(yearsAgo == 1 ? "year" : "years")} ago";
            }

            memoryAssets.AddRange(assets);
        }

        return memoryAssets;
    }

    private IEnumerable<MemoryType> EnabledMemoryTypes()
    {
        if (accountSettings.ShowOnThisDayMemories)
        {
            yield return MemoryType.On_this_day;
        }

        if (accountSettings.ShowBirthdayMemories)
        {
            yield return MemoryType.Birthday;
        }
    }
}

class DailyApiCache : ApiCache
{
    public DailyApiCache() : base(() => new MemoryCacheEntryOptions
    {
        AbsoluteExpiration = DateTimeOffset.Now.Date.AddDays(1)
    }
    )
    {
    }
}