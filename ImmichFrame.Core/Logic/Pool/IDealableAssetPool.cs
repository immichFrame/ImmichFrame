namespace ImmichFrame.Core.Logic.Pool;

/// <summary>A pool that can list every asset it selects and load assets by ID, so they can be dealt from a deck.</summary>
public interface IDealableAssetPool : IAssetPool
{
    /// <summary>
    /// The full current ID set. Returns the same listing until the underlying cache refreshes;
    /// a refreshed listing carries a higher <see cref="AssetIdList.Version"/>.
    /// </summary>
    Task<AssetIdList> GetAllAssetIds(CancellationToken ct = default);

    /// <summary>
    /// Loads these assets in the given order. Assets that are gone or no longer selected are left
    /// out; assets that failed for a passing reason are listed in <see cref="AssetLoadResult.Retry"/>.
    /// </summary>
    Task<AssetLoadResult> LoadAssetsByIds(IReadOnlyList<Guid> ids, CancellationToken ct = default);
}
