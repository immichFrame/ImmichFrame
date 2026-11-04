using System.Collections.Concurrent;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImmichFrame.Core.Logic.Pool;

public class AccountSearchPool : IDealableAssetPool, IDisposable
{
    /// <summary>
    /// The shortest time an ID listing is kept. Listing pages through the whole selection, so even
    /// with <c>RefreshAlbumPeopleInterval: 0</c> it runs at most this often.
    /// </summary>
    public static readonly TimeSpan MinIdListLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long a loaded asset (or the news that it is gone) is kept, so dealing it again soon,
    /// to another frame or after a deck wraps, costs no search.
    /// </summary>
    public static readonly TimeSpan LoadCacheLifetime = TimeSpan.FromMinutes(3);

    /// <summary>The most asset loads that run at once for this pool, across every caller.</summary>
    public const int MaxParallelLoads = 4;

    /// <summary>
    /// After this many 400/403 answers in a row with no successful load, the API key looks to have
    /// lost access: a warning is logged and refused assets stay in the deck. Fewer are taken to be
    /// deleted or unshared assets and are dropped.
    /// </summary>
    public const int RefusalsBeforeWarning = 5;

    // The largest page the metadata search allows.
    private const int IdPageSize = 1000;

    // Pages a listing may take beyond what the account's asset count needs, for assets added meanwhile.
    private const int ExtraIdPages = 10;

    // Bounds the load cache. When full, expired entries are dropped; new ones wait for room.
    private const int MaxCachedLoads = 4096;

    private readonly IApiCache _apiCache;
    private readonly ImmichApi _immichApi;
    private readonly IAccountSettings _accountSettings;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private readonly RefreshingValue<AssetIdList> _ids;
    private readonly SemaphoreSlim _loadSlots = new(MaxParallelLoads, MaxParallelLoads);

    private readonly object _loadCacheGate = new();
    private readonly Dictionary<Guid, CachedLoad> _loadCache = new();

    // Listings are versioned in load order. The empty listing, used while the filter selects
    // nothing, is the oldest, so a deck never prefers it over a real one.
    private readonly AssetIdList _noIds = new([], 0);
    private long _lastListingVersion;

    // 400/403 answers since the last successful load, across calls.
    private int _refusalsInARow;

    // A null asset means it is gone or no longer selected.
    private sealed record CachedLoad(AssetResponseDto? Asset, DateTimeOffset LoadedAt);

    public AccountSearchPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings,
        TimeSpan? idListLifetime = null, TimeProvider? timeProvider = null, ILogger? logger = null)
    {
        _apiCache = apiCache;
        _immichApi = immichApi;
        _accountSettings = accountSettings;
        _logger = logger ?? NullLogger.Instance;
        _time = timeProvider ?? TimeProvider.System;
        var lifetime = idListLifetime is { } requested && requested > MinIdListLifetime ? requested : MinIdListLifetime;
        _ids = new RefreshingValue<AssetIdList>(ListAssetIds, lifetime, _time, _logger, description: "asset ID listing");
    }

    /// <summary>How long an ID listing is kept before it is refreshed.</summary>
    public TimeSpan IdListLifetime => _ids.Lifetime;

    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        var tagIds = await ResolveTagIds(ct);
        if (!SelectsAnything(tagIds))
        {
            return 0;
        }

        var filter = SearchFilters.ForAccount(_accountSettings, tagIds);
        return await _apiCache.GetOrAddAsync("stats", async () =>
        {
            var stats = await _immichApi.SearchAssetStatisticsAsync(new StatisticsSearchDto
            {
                Filter = filter
            }, ct);
            return stats.Total;
        });
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, string clientId = "", CancellationToken ct = default)
    {
        var tagIds = await ResolveTagIds(ct);
        if (!SelectsAnything(tagIds))
        {
            return [];
        }

        return await _immichApi.SearchRandomAsync(new RandomSearchDto
        {
            Size = requested,
            WithExif = true,
            WithPeople = true,
            Filter = SearchFilters.ForAccount(_accountSettings, tagIds)
        }, ct);
    }

    public async Task<AssetIdList> GetAllAssetIds(CancellationToken ct = default)
    {
        var tagIds = await ResolveTagIds(ct);
        if (!SelectsAnything(tagIds))
        {
            return _noIds;
        }

        return await _ids.GetAsync(ct);
    }

    // One listing at a time per pool, shared by every caller; ct is the pool's own token.
    private async Task<AssetIdList> ListAssetIds(CancellationToken ct)
    {
        var filter = SearchFilters.ForAccount(_accountSettings, await ResolveTagIds(ct));
        // Bounds the paging, so a server that keeps handing out cursors cannot keep the listing going forever.
        var stats = await _immichApi.SearchAssetStatisticsAsync(new StatisticsSearchDto { Filter = filter }, ct);
        var maxPages = Math.Max(stats.Total, 0) / IdPageSize + ExtraIdPages;
        var ids = new List<Guid>();
        string? cursor = null;
        var pages = 0;
        do
        {
            if (++pages > maxPages)
            {
                throw new InvalidOperationException($"Immich returned more than {maxPages} pages of asset IDs for {stats.Total} assets; giving up on this listing");
            }

            var response = await _immichApi.SearchAssetsAsync(null, null, new MetadataSearchDto
            {
                Filter = filter,
                Size = IdPageSize,
                Cursor = cursor,
                WithExif = false,
                WithPeople = false
            }, ct);

            ids.AddRange(response.Assets.Items.Select(a => a.Id));
            cursor = response.Assets.NextCursor;
        } while (!string.IsNullOrEmpty(cursor));

        // Listings run one at a time, so this follows load order.
        return new AssetIdList(ids, Interlocked.Increment(ref _lastListingVersion));
    }

    /// <summary>
    /// Loads each asset through the account's filter plus an ID filter, so an asset that has left
    /// the selection since the listing (removed from the album, unfavourited, archived, retagged,
    /// trashed) is left out rather than shown. Every load costs one Immich search, so results are
    /// kept for <see cref="LoadCacheLifetime"/> and at most <see cref="MaxParallelLoads"/> run at
    /// once across all callers.
    /// </summary>
    public async Task<AssetLoadResult> LoadAssetsByIds(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var tagIds = await ResolveTagIds(ct);
        if (!SelectsAnything(tagIds))
        {
            return AssetLoadResult.Of([]);
        }

        var loaded = new AssetResponseDto?[ids.Count];
        var retry = new ConcurrentBag<int>();
        Exception? firstFailure = null;
        var failures = 0;
        await Parallel.ForEachAsync(Enumerable.Range(0, ids.Count),
            new ParallelOptions { MaxDegreeOfParallelism = MaxParallelLoads, CancellationToken = ct },
            async (i, token) =>
            {
                if (TryGetCachedLoad(ids[i], out var cached))
                {
                    loaded[i] = cached;
                    return;
                }

                try
                {
                    loaded[i] = await LoadSelected(ids[i], tagIds, token);
                    CacheLoad(ids[i], loaded[i]);
                    Interlocked.Exchange(ref _refusalsInARow, 0);
                }
                catch (ApiException e) when (e.StatusCode is 404)
                {
                    // Deleted since the listing.
                    CacheLoad(ids[i], null);
                }
                catch (ApiException e) when (e.StatusCode is 400 or 403)
                {
                    // Usually deleted, locked or no longer shared, so dropped. Not cached, in case it
                    // is a passing permission problem. A long run of these looks like the API key
                    // lost access: then the assets stay in the deck and the problem is logged once.
                    _logger.LogDebug("Immich answered {status} when loading asset {assetId}; skipping it", e.StatusCode, ids[i]);
                    var refusals = Interlocked.Increment(ref _refusalsInARow);
                    if (refusals >= RefusalsBeforeWarning)
                    {
                        retry.Add(i);
                    }

                    if (refusals == RefusalsBeforeWarning)
                    {
                        _logger.LogWarning("Immich refused the last {count} asset loads (400/403). Check that the API key can still read the configured albums, people and tags; the assets stay in the deck.", refusals);
                    }
                }
                catch (Exception e) when (!token.IsCancellationRequested)
                {
                    Interlocked.CompareExchange(ref firstFailure, e, null);
                    Interlocked.Increment(ref failures);
                    retry.Add(i);
                }
            });

        // Logged once per batch, so an outage does not log a line per asset.
        if (firstFailure != null)
        {
            _logger.LogWarning(firstFailure, "Could not load {count} of {total} assets; they will be dealt again later", failures, ids.Count);
        }

        return new AssetLoadResult(
            loaded.OfType<AssetResponseDto>().ToList(),
            retry.Order().Select(i => ids[i]).ToList());
    }

    private async Task<AssetResponseDto?> LoadSelected(Guid id, IReadOnlyCollection<Guid> tagIds, CancellationToken ct)
    {
        var filter = SearchFilters.ForAccount(_accountSettings, tagIds);
        filter.Id = new IdFilter { Eq = id };
        SearchResponseDto response;
        await _loadSlots.WaitAsync(ct);
        try
        {
            response = await _immichApi.SearchAssetsAsync(null, null, new MetadataSearchDto
            {
                Filter = filter,
                Size = 1,
                WithExif = true,
                WithPeople = true
            }, ct);
        }
        finally
        {
            _loadSlots.Release();
        }

        var asset = response.Assets.Items.FirstOrDefault(a => a.Id == id);
        return asset is { IsTrashed: not true } ? asset : null;
    }

    private bool TryGetCachedLoad(Guid id, out AssetResponseDto? asset)
    {
        lock (_loadCacheGate)
        {
            if (_loadCache.TryGetValue(id, out var entry))
            {
                if (_time.GetUtcNow() - entry.LoadedAt < LoadCacheLifetime)
                {
                    asset = entry.Asset;
                    return true;
                }

                _loadCache.Remove(id);
            }
        }

        asset = null;
        return false;
    }

    private void CacheLoad(Guid id, AssetResponseDto? asset)
    {
        lock (_loadCacheGate)
        {
            var now = _time.GetUtcNow();
            if (_loadCache.Count >= MaxCachedLoads && !_loadCache.ContainsKey(id))
            {
                foreach (var expired in _loadCache.Where(e => now - e.Value.LoadedAt >= LoadCacheLifetime).Select(e => e.Key).ToList())
                {
                    _loadCache.Remove(expired);
                }

                if (_loadCache.Count >= MaxCachedLoads)
                {
                    return;
                }
            }

            _loadCache[id] = new CachedLoad(asset, now);
        }
    }

    private bool SelectsAnything(IReadOnlyCollection<Guid> tagIds) =>
        !SearchFilters.HasConfiguredSource(_accountSettings) || SearchFilters.HasMatchingBranch(_accountSettings, tagIds);

    private async Task<IReadOnlyList<Guid>> ResolveTagIds(CancellationToken ct)
    {
        if (_accountSettings.Tags is not { Count: > 0 } values)
        {
            return [];
        }

        var allTags = await _apiCache.GetOrAddAsync($"allTags_{_accountSettings.ImmichServerUrl}",
            () => _immichApi.GetAllTagsAsync(ct));
        var tagValueToTag = allTags.ToDictionary(t => t.Value);
        return values
            .Where(tagValueToTag.ContainsKey)
            .Select(value => tagValueToTag[value].Id)
            .ToList();
    }

    // _loadSlots is not disposed: a load still running may yet release it.
    public void Dispose() => _ids.Dispose();
}
