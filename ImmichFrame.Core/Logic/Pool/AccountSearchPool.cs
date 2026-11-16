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

        var filter = SearchFilters.ForAccount(accountSettings, tagIds);
        return await apiCache.GetOrAddAsync(StatsCacheKey(), async () =>
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
            Filter = SearchFilters.ForAccount(accountSettings, tagIds)
        }, ct);
    }

    // Album overrides from the URL share the account's cache, so their counts need their own key.
    private string StatsCacheKey() => accountSettings.Albums is { Count: > 0 } albums
        ? $"stats_{string.Join(",", albums.Order())}"
        : "stats";

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
