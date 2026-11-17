using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            try
            {
                memories.AddRange(await immichApi.SearchMemoriesAsync(searchDate, null, null, null, null, null, null, null, type, ct));
            }
            catch (ApiException ex) when (type != MemoryType.On_this_day && IsUnsupportedTypeError(ex))
            {
                // Older servers reject memory types they don't know (v3.2 has no "birthday").
                // Skip that type instead of losing the on-this-day memories with it.
            }
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

    // Only a request-validation failure on the `type` parameter counts. Immich answers those with
    // {"message": "Validation failed", "errors": [{"path": ["type"], ...}]}. Any other 400 (a proxy,
    // a bad date) must propagate: a swallowed error would be cached as "no memories" until midnight.
    private static bool IsUnsupportedTypeError(ApiException ex)
    {
        if (ex.StatusCode != 400 || string.IsNullOrEmpty(ex.Response))
        {
            return false;
        }

        try
        {
            return JObject.Parse(ex.Response)["errors"] is JArray errors
                && errors.Any(e => e["path"] is JArray path && path.Count == 1 && (string?)path[0] == "type");
        }
        catch (JsonReaderException)
        {
            return false;
        }
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