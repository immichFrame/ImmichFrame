using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.Core.Logic.Pool;

public class MemoryAssetsPool(ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(new DailyApiCache(), accountSettings)
{
    protected override async Task<IList<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var memories = await immichApi.SearchMemoriesAsync(DateTime.Now, null, null, null, ct);

        var memoryAssets = new List<AssetResponseDto>();
        foreach (var memory in memories.OrderBy(m => m.MemoryAt))
        {
            var assets = memory.Assets.ToList();
            var yearsAgo = DateTime.Now.Year - memory.Data.Year;

            foreach (var asset in assets)
            {
                if (asset.ExifInfo == null)
                {
                    var assetInfo = await immichApi.GetAssetInfoAsync(new Guid(asset.Id), null, ct);
                    asset.ExifInfo = assetInfo.ExifInfo;
                    asset.People = assetInfo.People;
                }
                asset.ExifInfo.Description = $"{yearsAgo} {(yearsAgo == 1 ? "year" : "years")} ago";
            }

            memoryAssets.AddRange(assets);
        }

        return memoryAssets;
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