using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class AllAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : IAssetPool
{
    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        //Retrieve total images count (unfiltered); will update to query filtered stats from Immich
        return (await apiCache.GetOrAddAsync(nameof(AllAssetsPool),
            () => immichApi.GetAssetStatisticsAsync(null, false, null, ct))).Images;
    }
    
    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        var searchDto = new RandomSearchDto
            {
                Size = requested,
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                WithPeople = true
            };

            if (accountSettings.ShowArchived)
            {
                searchDto.Visibility = AssetVisibility.Archive;
            }
            else
            {
                searchDto.Visibility = AssetVisibility.Timeline;
            }

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