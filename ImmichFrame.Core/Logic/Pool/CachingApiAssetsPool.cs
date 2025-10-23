using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public abstract class CachingApiAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : IAssetPool
{
    private readonly Random _random = new();
    
    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        return (await AllAssets(ct)).Count();
    }
    
    public virtual async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        // Randomize the order of assets in the cache may destroy logic in other pools if they rely on order
        // For example, PeopleAssetsPool relies on the order of assets to show the most recent
        // So we should not randomize here
        //
        return (await AllAssets(ct)).OrderBy(_ => _random.Next()).Take(requested);
    }

    protected async Task<IEnumerable<AssetResponseDto>> AllAssets(CancellationToken ct = default)
    {
        return await apiCache.GetOrAddAsync(GetType().FullName!, () => ApplyAccountFilters(LoadAssets(ct)));
    }


    protected async Task<IEnumerable<AssetResponseDto>> ApplyAccountFilters(Task<IEnumerable<AssetResponseDto>> unfiltered)
    {
        // Display only Images
        var assets = (await unfiltered).Where(x => x.Type == AssetTypeEnum.IMAGE);

        if (!accountSettings.ShowArchived)
            assets = assets.Where(x => x.IsArchived == false);

        var takenBefore = accountSettings.ImagesUntilDate.HasValue ? accountSettings.ImagesUntilDate : null;
        if (takenBefore.HasValue)
        {
            assets = assets.Where(x => x.ExifInfo.DateTimeOriginal <= takenBefore);
        }

        var takenAfter = accountSettings.ImagesFromDate.HasValue ? accountSettings.ImagesFromDate : accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-accountSettings.ImagesFromDays.Value) : null;
        if (takenAfter.HasValue)
        {
            assets = assets.Where(x => x.ExifInfo.DateTimeOriginal >= takenAfter);
        }

        if (accountSettings.Rating is int rating)
        {
            assets = assets.Where(x => x.ExifInfo.Rating == rating);
        }
        
        return assets;
    }
        
    protected abstract Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default);
}