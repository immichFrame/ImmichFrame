using ImmichFrame.Core.Api;

namespace ImmichFrame.Core.Logic.Pool;

/// <summary>
/// Decorates an asset pool to serve assets without repetition ("exhaustive shuffle").
/// It draws the underlying set, shuffles it (Fisher-Yates), and serves each asset
/// exactly once before reshuffling, so every photo is shown before any repeats.
/// Resolves https://github.com/immichFrame/ImmichFrame/issues/438.
///
/// Notes:
/// - Single-client semantics: one bag is shared per pool instance.
/// - The bag is rebuilt from a fresh draw whenever it empties, so newly added
///   assets are picked up on the next cycle without restarting the frame.
/// - Exhaustive for single-source pools (all-assets, a single album, a person, ...).
///   For multi-source aggregates it dedupes by id and is best-effort per cycle.
/// - For very large libraries the bag is capped at <see cref="MaxBagSize"/>, turning
///   each cycle into a rolling no-repeat window rather than the entire library.
///   The cap also matches Immich's maximum random-search page size (1000).
/// </summary>
public class ShuffleBagAssetPool(IAssetPool inner) : AggregatingAssetPool
{
    // Immich's random search rejects size > 1000, so a single draw cannot exceed it.
    private const int MaxBagSize = 1000;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<AssetResponseDto> _bag = new();
    private int _index;

    public override Task<long> GetAssetCount(CancellationToken ct = default) => inner.GetAssetCount(ct);

    protected override async Task<AssetResponseDto?> GetNextAsset(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_index >= _bag.Count)
            {
                await RefillAsync(ct);
                if (_bag.Count == 0) return null;
            }

            return _bag[_index++];
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RefillAsync(CancellationToken ct)
    {
        _index = 0;
        _bag = new List<AssetResponseDto>();

        var count = await inner.GetAssetCount(ct);
        if (count <= 0) return;

        var requested = (int)Math.Min(count, MaxBagSize);
        var seen = new HashSet<string>();
        var fresh = new List<AssetResponseDto>(requested);

        foreach (var asset in await inner.GetAssets(requested, ct))
        {
            if (seen.Add(asset.Id))
            {
                fresh.Add(asset);
            }
        }

        Shuffle(fresh);
        _bag = fresh;
    }

    private static void Shuffle(IList<AssetResponseDto> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
