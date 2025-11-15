using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public abstract class CachingApiAssetsPool(IApiCache apiCache, IAccountSettings accountSettings) : IAssetPool
{
    private int _next; //next asset to return

    public async Task<long> GetAssetCount(CancellationToken ct = default)
        => (await AllAssets(ct)).Count;
    
    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        if (requested == 0)
        {
            return new List<AssetResponseDto>();
        }
        
        var all = await AllAssets(ct);

        if (all.Count < requested)
        {
            requested = all.Count; //limit request to what we have
        }
        
        var tail = all.TakeLast(all.Count - _next).ToList();
        
        if (tail.Count >= requested)
        {
            _next += requested;
            return tail.Take(requested);
        }

        // not enough left in tail; need to read head too
        var overrun = requested - tail.Count;
        _next = overrun;
        return tail.Concat(all.Take(overrun));
    }

    private async Task<IList<AssetResponseDto>> AllAssets(CancellationToken ct = default)
    {
        return await apiCache.GetOrAddAsync(GetType().FullName!, () => ApplyAccountFilters(LoadAssets(ct)));
    }


    protected async Task<IList<AssetResponseDto>> ApplyAccountFilters(Task<IList<AssetResponseDto>> unfiltered)
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

        return assets.ToList();
    }

    protected abstract Task<IList<AssetResponseDto>> LoadAssets(CancellationToken ct = default);
}