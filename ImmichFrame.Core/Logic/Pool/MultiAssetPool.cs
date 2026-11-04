using ImmichFrame.Core.Api;

namespace ImmichFrame.Core.Logic.Pool;

public class MultiAssetPool(IEnumerable<IAssetPool> delegates) : AggregatingAssetPool
{
    public override async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        var counts = delegates.Select(pool => pool.GetAssetCount(ct));
        return (await Task.WhenAll(counts)).Sum();
    }

    /// <summary>
    /// Picks a source for every slot of the batch, weighted by each source's asset count, then asks
    /// each source once for all of its slots. The batch keeps the drawn order; a source that returns
    /// fewer than asked leaves its remaining slots empty.
    /// </summary>
    public override async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, string clientId = "", CancellationToken ct = default)
    {
        if (requested <= 0)
        {
            return [];
        }

        var pools = delegates.ToList();
        var counts = await Task.WhenAll(pools.Select(pool => pool.GetAssetCount(ct)));
        var total = counts.Where(c => c > 0).Sum();
        if (total <= 0)
        {
            return [];
        }

        var slots = new int[requested];
        var perPool = new int[pools.Count];
        for (var slot = 0; slot < requested; slot++)
        {
            var pick = Random.Shared.NextInt64(total);
            var chosen = 0;
            while (counts[chosen] <= 0 || pick >= counts[chosen])
            {
                pick -= Math.Max(counts[chosen], 0);
                chosen++;
            }

            slots[slot] = chosen;
            perPool[chosen]++;
        }

        var batches = await Task.WhenAll(pools.Select((pool, i) => perPool[i] > 0
            ? pool.GetAssets(perPool[i], clientId, ct)
            : Task.FromResult(Enumerable.Empty<AssetResponseDto>())));
        var queues = batches.Select(batch => new Queue<AssetResponseDto>(batch)).ToList();

        var assets = new List<AssetResponseDto>(requested);
        foreach (var slot in slots)
        {
            if (queues[slot].TryDequeue(out var asset))
            {
                assets.Add(asset);
            }
        }

        return assets;
    }

    protected override async Task<AssetResponseDto?> GetNextAsset(string clientId, CancellationToken ct)
        => (await GetAssets(1, clientId, ct)).FirstOrDefault();
}
