using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic.Pool.Remote;

public class FilteredAssetsRemotePool(
    IApiCache apiCache,
    ImmichApi immichApi,
    IAccountSettings accountSettings,
    ILogger<FilteredAssetsRemotePool> logger) : BaseCircuitBreaker(logger), IAssetPool
{
    private readonly object _filteredCacheKey = new();
    private readonly object _totalCacheKey = new();

    public virtual async Task<long> GetAssetCount(CancellationToken ct = default)
        => await DoCall(
            () => GetFilteredAssetCount(ct),
            () => GetTotalAssetCount(ct));

    private async Task<long> GetFilteredAssetCount(CancellationToken ct = default)
        => (await apiCache.GetOrAddAsync(_filteredCacheKey,
            () => immichApi.SearchAssetStatisticsAsync(BuildCountQuery(), ct))).Total;

    private async Task<long> GetTotalAssetCount(CancellationToken ct = default)
        => (await apiCache.GetOrAddAsync(_totalCacheKey,
            () => immichApi.GetAssetStatisticsAsync(null, false, null, ct))).Images;

    private RandomSearchDto BuildAssetQuery(int requested) => new()
    {
        Size = requested,
        Type = AssetTypeEnum.IMAGE,
        AlbumIds = accountSettings.Albums,
        PersonIds = accountSettings.People,
        IsFavorite = accountSettings.ShowFavorites,
        WithExif = true,
        WithPeople = true,
        Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline,
        TakenBefore = accountSettings.ImagesUntilDate,
        Rating = accountSettings.Rating,
        TakenAfter = accountSettings.ImagesFromDate ?? (accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-accountSettings.ImagesFromDays.Value) : null)
    };

    private StatisticsSearchDto BuildCountQuery() => new()
    {
        Type = AssetTypeEnum.IMAGE,
        AlbumIds = accountSettings.Albums,
        PersonIds = accountSettings.People,
        IsFavorite = accountSettings.ShowFavorites,
        Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline,
        TakenBefore = accountSettings.ImagesUntilDate,
        Rating = accountSettings.Rating,
        TakenAfter = accountSettings.ImagesFromDate ?? (accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-accountSettings.ImagesFromDays.Value) : null)
    };

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        var assets = await immichApi.SearchRandomAsync(BuildAssetQuery(requested), ct);

        if (accountSettings.ExcludedAlbums.Count == 0) return assets;

        var excludedAssetList = await GetExcludedAlbumAssets(ct);
        var excludedAssetSet = excludedAssetList.Select(x => x.Id).ToHashSet();
        assets = assets.Where(x => !excludedAssetSet.Contains(x.Id)).ToList();

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