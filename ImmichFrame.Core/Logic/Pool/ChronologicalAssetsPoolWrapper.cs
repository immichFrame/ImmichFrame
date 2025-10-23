using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

/// <summary>
/// Wraps an IAssetPool to provide chronological asset grouping functionality.
/// This wrapper organizes assets into chronological sets based on their capture dates,
/// while randomizing the order of sets to maintain variety in display. Assets without
/// date information are preserved and placed after chronologically ordered assets.
/// Fetches assets in configurable batch sizes for optimal chronological sorting performance.
/// </summary>
/// <remarks>
/// The wrapper operates by:
/// 1. Fetching a larger pool of assets (configurable multiplier with cap)
/// 2. Separating assets with and without DateTimeOriginal metadata
/// 3. Sorting dated assets chronologically
/// 4. Combining dated and undated assets (dated first)
/// 5. Creating consecutive sets of specified size
/// 6. Randomizing set order while preserving internal chronological order
/// </remarks>
public class ChronologicalAssetsPoolWrapper(IAssetPool basePool, IGeneralSettings generalSettings) : IAssetPool
{
    /// <summary>
    /// Default multiplier applied to requested asset count for fetching a larger pool.
    /// This ensures sufficient assets are available for chronological grouping.
    /// </summary>
    private const int DefaultFetchMultiplier = 10;

    /// <summary>
    /// Maximum number of assets to fetch to prevent excessive memory usage.
    /// </summary>
    private const int MaxFetchCount = 1000;

    private readonly IAssetPool _basePool = basePool;
    private readonly IGeneralSettings _generalSettings = generalSettings;

    /// <summary>
    /// Gets a collection of assets organized in chronological sets.
    /// This method fetches assets from the base pool and organizes them chronologically
    /// if chronological features are enabled. If disabled, it falls back to the base pool.
    /// </summary>
    /// <param name="requested">The number of assets to return.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An enumerable collection of assets organized chronologically.</returns>
    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        if (_generalSettings.ChronologicalImagesCount <= 0)
        {
            // Fallback to base pool behavior if chronological is disabled
            return await _basePool.GetAssets(requested, ct);
        }

        var chronologicalCount = _generalSettings.ChronologicalImagesCount;

        // Calculate how many assets to fetch for proper chronological grouping.
        // Use a larger pool to ensure good variety and enough assets for chronological sets.
        var fetchCount = Math.Min(MaxFetchCount, requested * DefaultFetchMultiplier);

        // Get available assets from base pool
        var availableAssets = await _basePool.GetAssets(fetchCount, ct);
        var assetsList = availableAssets.ToList();
        if (assetsList.Count == 0)
        {
            return assetsList;
        }

        // Separate assets with and without date information
        var (datedAssets, undatedAssets) = SeparateDateAndNonDateAssets(assetsList);

        // Sort dated assets chronologically
        var sortedDatedAssets = SortAssetsChronologically(datedAssets);

        // Combine dated and undated assets (dated first to maximize chronological grouping)
        var combinedAssets = sortedDatedAssets.Concat(undatedAssets).ToList();

        // Create chronological sets and randomize their order
        var chronologicalSets = CreateChronologicalSets(combinedAssets, chronologicalCount);
        var randomizedSets = RandomizeSets(chronologicalSets);
        // Flatten and return the requested number of assets
        return randomizedSets.SelectMany(set => set).Take(requested);
    }

    /// <summary>
    /// Gets the total count of assets available from the base pool.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The total number of assets available.</returns>
    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        return await _basePool.GetAssetCount(ct);
    }

    /// <summary>
    /// Separates assets into those with valid dates and those without.
    /// Uses DateTimeOriginal metadata with validation for reasonable date ranges.
    /// </summary>
    /// <param name="assets">The collection of assets to separate.</param>
    /// <returns>A tuple containing dated assets and undated assets.</returns>
    private (List<AssetResponseDto> datedAssets, List<AssetResponseDto> undatedAssets) SeparateDateAndNonDateAssets(
        IEnumerable<AssetResponseDto> assets)
    {
        var datedAssets = new List<AssetResponseDto>();
        var undatedAssets = new List<AssetResponseDto>();

        foreach (var asset in assets)
        {
            if (TryParseDateTime(asset, out _))
            {
                datedAssets.Add(asset);
            }
            else
            {
                undatedAssets.Add(asset);
            }
        }

        return (datedAssets, undatedAssets);
    }

    /// <summary>
    /// Attempts to parse a DateTime from an asset using DateTimeOriginal with validation.
    /// Only accepts dates within a reasonable range (after 1826 and not in the future).
    /// </summary>
    /// <param name="asset">The asset to parse the date from.</param>
    /// <param name="parsedDate">The successfully parsed date, or default if parsing failed.</param>
    /// <returns>True if a valid date was parsed, false otherwise.</returns>
    private static bool TryParseDateTime(AssetResponseDto asset, out DateTime parsedDate)
    {
        try
        {
            parsedDate = default;
            var dateValue = default(DateTime);
            // empty asset check
            if (asset == null)
            {
                return false;
            }

            // Try DateTimeOriginal with validation
            if (asset.ExifInfo?.DateTimeOriginal.HasValue == true)
            {
                dateValue = asset.ExifInfo.DateTimeOriginal.Value.DateTime;
                parsedDate = dateValue;
                return true;
            }

            // Fallback to FileCreatedAt if DateTimeOriginal is not available
            dateValue = asset.FileCreatedAt.DateTime;
            parsedDate = dateValue;
            return true;
        }
        catch
        {
            // On any error, treat as undated            
            parsedDate = default;
            return false;
        }
    }

    /// <summary>
    /// Sorts assets chronologically using DateTimeOriginal metadata.
    /// </summary>
    /// <param name="assets">The assets to sort.</param>
    /// <returns>Assets sorted by chronological order.</returns>
    private static List<AssetResponseDto> SortAssetsChronologically(IEnumerable<AssetResponseDto> assets)
    {
        var assetsWithDates = new List<(AssetResponseDto Asset, DateTime Date)>();
        // Extract sorting dates and pair with assets
        foreach (var asset in assets)
        {
            if (TryParseDateTime(asset, out var date))
            {
                assetsWithDates.Add((asset, date));
            }
        }
        var retlist = assetsWithDates.OrderBy(x => x.Date).Select(x => x.Asset).ToList();
        return retlist;
    }

    /// <summary>
    /// Creates chronological sets from a sorted list of assets.
    /// Each set contains a specified number of consecutive assets.
    /// </summary>
    /// <param name="assets">The chronologically sorted assets.</param>
    /// <param name="setSize">The number of assets per set.</param>
    /// <returns>A list of asset sets.</returns>
    private static List<List<AssetResponseDto>> CreateChronologicalSets(
        IReadOnlyList<AssetResponseDto> assets, int setSize)
    {
        var sets = new List<List<AssetResponseDto>>();
        var setAssets = new List<AssetResponseDto>(setSize);

        foreach (var item in assets)
        {   
            // add actual item
            setAssets.Add(item);
            // check if we reached the set size
            if (setAssets.Count >= setSize)
            {
                // add actual set to sets
                sets.Add(setAssets);
                // init new list if setSize is reached
                setAssets = new List<AssetResponseDto>(setSize);
            }
        }

        // Add the remaining assets as the last set (if any)
        if (setAssets.Count > 0)
        {
            sets.Add(setAssets);
        }

        return sets;
    }

    /// <summary>
    /// Randomizes the order of asset sets while preserving the chronological order within each set.
    /// </summary>
    /// <param name="sets">The chronological sets to randomize.</param>
    /// <returns>Sets in randomized order.</returns>
    private static List<List<AssetResponseDto>> RandomizeSets(IEnumerable<List<AssetResponseDto>> sets)
    {
        var setsList = sets.ToList();
        var random = new Random();
        
        // Fisher-Yates shuffle algorithm to properly randomize set order
        for (var i = setsList.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (setsList[i], setsList[j]) = (setsList[j], setsList[i]);
        }
        
        return setsList;
    }
}