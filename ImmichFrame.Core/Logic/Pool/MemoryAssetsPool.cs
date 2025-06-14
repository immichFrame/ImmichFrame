using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class MemoryAssetsPool(ApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : CachingApiAssetsPool(apiCache, immichApi, accountSettings)
{
    protected override async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var memories = await immichApi.SearchMemoriesAsync(DateTime.Now, null, null, null, ct);

        var memoryAssets = new List<AssetResponseDto>();
        foreach (var memory in memories)
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