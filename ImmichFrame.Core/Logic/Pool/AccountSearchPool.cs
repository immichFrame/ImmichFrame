using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class AccountSearchPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : IAssetPool
{
    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        var tagIds = await ResolveTagIds(ct);
        if (SearchFilters.HasConfiguredSource(accountSettings) && !SearchFilters.HasMatchingBranch(accountSettings, tagIds))
        {
            return 0;
        }

        var filter = SearchFilters.ForAccount(accountSettings, tagIds, await ResolveExcludedAlbumIds(ct));
        return await apiCache.GetOrAddAsync("stats", async () =>
        {
            var stats = await immichApi.SearchAssetStatisticsAsync(new StatisticsSearchDto
            {
                Filter = filter
            }, ct);
            return stats.Total;
        });
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        var tagIds = await ResolveTagIds(ct);
        if (SearchFilters.HasConfiguredSource(accountSettings) && !SearchFilters.HasMatchingBranch(accountSettings, tagIds))
        {
            return [];
        }

        return await immichApi.SearchRandomAsync(new RandomSearchDto
        {
            Size = requested,
            WithExif = true,
            WithPeople = true,
            Filter = SearchFilters.ForAccount(accountSettings, tagIds, await ResolveExcludedAlbumIds(ct))
        }, ct);
    }

    /// <summary>
    /// The configured ExcludedAlbums plus, when HideAssetsInOtherAlbums is set, every album
    /// (owned or shared) that is not one of the selected Albums. Immich applies them as a
    /// "none of these albums" filter, so an asset that also sits in another album is skipped.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> ResolveExcludedAlbumIds(CancellationToken ct)
    {
        var excluded = accountSettings.ExcludedAlbums ?? [];

        // Without selected albums every album would count as "other", hiding every asset in an album
        if (!accountSettings.HideAssetsInOtherAlbums || accountSettings.Albums is not { Count: > 0 } selected)
        {
            return excluded;
        }

        var selectedIds = selected.ToHashSet();
        var allAlbums = await apiCache.GetOrAddAsync($"allAlbums_{accountSettings.ImmichServerUrl}",
            () => immichApi.GetAllAlbumsAsync(null, null, null, null, null, ct));

        return excluded
            .Concat(allAlbums.Select(album => album.Id).Where(id => !selectedIds.Contains(id)))
            .Distinct()
            .ToList();
    }

    private async Task<IReadOnlyList<Guid>> ResolveTagIds(CancellationToken ct)
    {
        if (accountSettings.Tags is not { Count: > 0 } values)
        {
            return [];
        }

        var allTags = await apiCache.GetOrAddAsync($"allTags_{accountSettings.ImmichServerUrl}",
            () => immichApi.GetAllTagsAsync(ct));
        var tagValueToTag = allTags.ToDictionary(t => t.Value);
        return values
            .Where(tagValueToTag.ContainsKey)
            .Select(value => tagValueToTag[value].Id)
            .ToList();
    }
}
