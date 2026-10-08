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

        return await apiCache.GetOrAddAsync("stats", () => SearchWithCurrentAlbums(async excludedAlbumIds =>
        {
            var stats = await immichApi.SearchAssetStatisticsAsync(new StatisticsSearchDto
            {
                Filter = SearchFilters.ForAccount(accountSettings, tagIds, excludedAlbumIds)
            }, ct);
            return stats.Total;
        }, ct));
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        var tagIds = await ResolveTagIds(ct);
        if (SearchFilters.HasConfiguredSource(accountSettings) && !SearchFilters.HasMatchingBranch(accountSettings, tagIds))
        {
            return [];
        }

        return await SearchWithCurrentAlbums<IEnumerable<AssetResponseDto>>(async excludedAlbumIds =>
            await immichApi.SearchRandomAsync(new RandomSearchDto
            {
                Size = requested,
                WithExif = true,
                WithPeople = true,
                Filter = SearchFilters.ForAccount(accountSettings, tagIds, excludedAlbumIds)
            }, ct), ct);
    }

    // Without selected albums every album would count as "other", hiding every asset in an album
    private bool HidesOtherAlbums => accountSettings.HideAssetsInOtherAlbums && accountSettings.Albums is { Count: > 0 };

    private string AllAlbumsCacheKey => $"allAlbums_{accountSettings.ImmichServerUrl}";

    /// <summary>
    /// Runs a search with the resolved excluded albums. Immich rejects the whole search with a 400
    /// when any album id in the filter is no longer accessible, so a cached album list that still
    /// names a deleted or unshared album is dropped and the search is retried once with a fresh one.
    /// </summary>
    private async Task<T> SearchWithCurrentAlbums<T>(Func<IReadOnlyList<Guid>, Task<T>> search, CancellationToken ct)
    {
        try
        {
            return await search(await ResolveExcludedAlbumIds(ct));
        }
        catch (ApiException ex) when (ex.StatusCode == 400 && HidesOtherAlbums)
        {
            apiCache.Remove(AllAlbumsCacheKey);
            return await search(await ResolveExcludedAlbumIds(ct));
        }
    }

    /// <summary>
    /// The configured ExcludedAlbums plus, when HideAssetsInOtherAlbums is set, every album
    /// (owned or shared) that is not one of the selected Albums. Immich applies them as a
    /// "none of these albums" filter, so an asset that also sits in another album is skipped.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> ResolveExcludedAlbumIds(CancellationToken ct)
    {
        var excluded = accountSettings.ExcludedAlbums ?? [];

        if (!HidesOtherAlbums)
        {
            return excluded;
        }

        var selectedIds = accountSettings.Albums.ToHashSet();
        var allAlbums = await apiCache.GetOrAddAsync(AllAlbumsCacheKey,
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
