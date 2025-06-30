using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool.Remote;

public class AllAssetsRemotePool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings, ILogger<AllAssetsRemotePool> logger) : BaseCircuitBreaker(logger), IAssetPool
{
    public virtual Task<long> GetAssetCount(CancellationToken ct = default)
        => DoCall(
            () => GetFilteredAssetCount(ct),
            () => GetTotalAssetCount(ct));
    
    private async Task<long> GetFilteredAssetCount(CancellationToken ct = default)
        => (await apiCache.GetOrAddAsync($"{nameof(AllAssetsRemotePool)}:filtered",
            () => immichApi.SearchAssetStatisticsAsync(new StatisticsSearchDto { Type = AssetTypeEnum.IMAGE }, ct))).Total;
    
    private async Task<long> GetTotalAssetCount(CancellationToken ct = default)
        => (await apiCache.GetOrAddAsync($"{nameof(AllAssetsRemotePool)}:total",
            () => immichApi.GetAssetStatisticsAsync(null, false, null, ct))).Images;
    
    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        var searchDto = new RandomSearchDto
        {
            Size = requested,
            Type = AssetTypeEnum.IMAGE,
            WithExif = true,
            WithPeople = true,
            Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline
        };

        var takenBefore = accountSettings.ImagesUntilDate.HasValue ? accountSettings.ImagesUntilDate : null;
        if (takenBefore.HasValue)
        {
            searchDto.TakenBefore = takenBefore;
        }

        var takenAfter = accountSettings.ImagesFromDate.HasValue ? accountSettings.ImagesFromDate : accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-accountSettings.ImagesFromDays.Value) : null;

        if (takenAfter.HasValue)
        {
            searchDto.TakenAfter = takenAfter;
        }

        if (accountSettings.Rating is int rating)
        {
            searchDto.Rating = rating;
        }

        var assets = await immichApi.SearchRandomAsync(searchDto, ct);

        if (accountSettings.ExcludedAlbums.Any())
        {
            var excludedAssetList = await GetExcludedAlbumAssets(ct);
            var excludedAssetSet = excludedAssetList.Select(x => x.Id).ToHashSet();
            assets = assets.Where(x => !excludedAssetSet.Contains(x.Id)).ToList();
        }

        return assets;
    }


    private async Task<IEnumerable<AssetResponseDto>> GetExcludedAlbumAssets(CancellationToken ct = default)
    {
        var excludedAlbumAssets = new List<AssetResponseDto>();

        foreach (var albumId in accountSettings.ExcludedAlbums)
        {
            var albumInfo = await immichApi.GetAlbumInfoAsync(albumId, null, null, ct);

            excludedAlbumAssets.AddRange(albumInfo.Assets);
        }

        return excludedAlbumAssets;
    }
}