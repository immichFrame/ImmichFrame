using System.Runtime.CompilerServices;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public abstract class CachingApiAssetsPool(IApiCache apiCache) : IDealableAssetPool
{
    private readonly Random _random = new();
    private readonly object _indexGate = new();
    private AssetIndex? _index;

    private readonly object _loadGate = new();
    private Task<IEnumerable<AssetResponseDto>>? _loading;

    // The order each loaded list was loaded in, so an index is never rebuilt from an older one.
    private readonly ConditionalWeakTable<IEnumerable<AssetResponseDto>, StrongBox<long>> _loadVersions = new();
    private long _lastLoadVersion;

    // IDs and a lookup built from one loaded list, so the listing stays the same instance (and
    // version) until a newer load replaces it.
    private sealed record AssetIndex(long Version, AssetIdList Ids, IReadOnlyDictionary<Guid, AssetResponseDto> ById);

    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        return (await AllAssets(ct)).Count();
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, string clientId = "", CancellationToken ct = default)
    {
        return (await AllAssets(ct)).OrderBy(_ => _random.Next()).Take(requested);
    }

    public async Task<AssetIdList> GetAllAssetIds(CancellationToken ct = default)
    {
        return (await GetIndex(ct)).Ids;
    }

    public async Task<AssetLoadResult> LoadAssetsByIds(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var index = await GetIndex(ct);
        return AssetLoadResult.Of(ids.Where(index.ById.ContainsKey).Select(id => index.ById[id]).ToList());
    }

    private async Task<AssetIndex> GetIndex(CancellationToken ct)
    {
        var all = await AllAssets(ct);
        var version = LoadVersionOf(all);
        lock (_indexGate)
        {
            if (_index != null && version <= _index.Version)
            {
                return _index;
            }
        }

        var byId = new Dictionary<Guid, AssetResponseDto>();
        foreach (var asset in all)
        {
            byId.TryAdd(asset.Id, asset);
        }

        var index = new AssetIndex(version, new AssetIdList(byId.Keys, version), byId);
        lock (_indexGate)
        {
            // Another caller may have built a newer index meanwhile.
            if (_index == null || index.Version > _index.Version)
            {
                _index = index;
            }

            return _index;
        }
    }

    private async Task<IEnumerable<AssetResponseDto>> AllAssets(CancellationToken ct = default)
    {
        return await apiCache.GetOrAddAsync(GetType().FullName!, () => LoadOnce().WaitAsync(ct));
    }

    // Callers that miss the cache together share one load, which no single caller can cancel.
    private Task<IEnumerable<AssetResponseDto>> LoadOnce()
    {
        lock (_loadGate)
        {
            return _loading ??= Task.Run(async () =>
            {
                try
                {
                    var assets = await LoadAssets(CancellationToken.None);
                    _loadVersions.AddOrUpdate(assets, new StrongBox<long>(Interlocked.Increment(ref _lastLoadVersion)));
                    return assets;
                }
                finally
                {
                    lock (_loadGate)
                    {
                        _loading = null;
                    }
                }
            });
        }
    }

    // A list that did not come through LoadOnce counts as loaded now.
    private long LoadVersionOf(IEnumerable<AssetResponseDto> assets) =>
        _loadVersions.GetValue(assets, _ => new StrongBox<long>(Interlocked.Increment(ref _lastLoadVersion))).Value;

    protected abstract Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default);
}
