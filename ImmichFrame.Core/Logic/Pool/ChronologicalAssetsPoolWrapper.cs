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
    /// Maximum number of assets to fetch in a single operation to prevent excessive memory usage.
    /// </summary>
    private const int MaxFetchCap = 1000;
    
    /// <summary>
    /// Gets the total count of assets available in the underlying pool.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The total number of assets available.</returns>
    public Task<long> GetAssetCount(CancellationToken ct = default)
    {
        return basePool.GetAssetCount(ct);
    }
    
    /// <summary>
    /// Retrieves assets organized into chronological sets when chronological display is enabled.
    /// Assets are grouped by capture date and presented in randomized set order while maintaining
    /// chronological order within each set. Falls back to standard pool behavior when disabled.
    /// </summary>
    /// <param name="requested">The number of assets to return.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An enumerable collection of assets organized chronologically.</returns>
    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        if (!generalSettings.ShowChronologicalImages || generalSettings.ChronologicalImagesCount <= 0)
        {
            // Fallback to base pool behavior if chronological is disabled
            return await basePool.GetAssets(requested, ct);
        }
        
        var chronologicalCount = generalSettings.ChronologicalImagesCount;
        
        // Get a larger set to work with for chronological selection
        // Use configurable multiplier and cap for better performance and flexibility
        var multiplier = Math.Max(DefaultFetchMultiplier, 1); // Ensure multiplier >= 1
        var fetchCount = Math.Min(requested * multiplier, MaxFetchCap);
        var availableAssets = await basePool.GetAssets(fetchCount, ct);
        
        // Create chronological sets and mix them randomly
        var chronologicalSets = CreateChronologicalSets(availableAssets, chronologicalCount);
        
        // Shuffle the sets randomly but keep chronological order within each set
        var shuffledSets = chronologicalSets.OrderBy(_ => Random.Shared.Next()).ToList();
        
        // Flatten the sets and take the requested amount
        var result = shuffledSets
            .SelectMany(set => set)
            .Take(requested)
            .ToList();
        
        return result;
    }    /// <summary>
    /// Creates chronological sets from the provided assets.
    /// Assets with DateTimeOriginal are sorted chronologically and placed first,
    /// followed by assets without date information to preserve all assets.
    /// When no assets have date information, all assets are randomly shuffled.
    /// </summary>
    /// <param name="allAssets">The collection of assets to organize into chronological sets.</param>
    /// <param name="setSize">The number of assets per chronological set.</param>
    /// <returns>An enumerable collection of chronological asset sets.</returns>
    /// <remarks>
    /// This method ensures no assets are lost by:
    /// - Chronologically ordering assets with DateTimeOriginal metadata
    /// - Appending assets without date metadata after the dated ones
    /// - Falling back to random ordering when no date information is available
    /// </remarks>
    private IEnumerable<IEnumerable<AssetResponseDto>> CreateChronologicalSets(IEnumerable<AssetResponseDto> allAssets, int setSize)
    {
        var assetsList = allAssets.ToList();
        
        // Separate assets with and without date information using safe parsing
        var datedAssets = assetsList
            .Select(a => new { Asset = a, ParsedDate = TryParseDateTime(a) })
            .Where(x => x.ParsedDate.HasValue)
            .OrderBy(x => x.ParsedDate!.Value)
            .Select(x => x.Asset)
            .ToList();
            
        var undatedAssets = assetsList
            .Where(a => !TryParseDateTime(a).HasValue)
            .ToList();
        
        if (!datedAssets.Any())
        {
            // Fallback: create sets from randomly ordered assets if no date info available
            var randomAssets = assetsList.OrderBy(_ => Random.Shared.Next()).ToList();
            return CreateSetsFromList(randomAssets, setSize);
        }
        
        // Combine dated assets first, then undated assets (preserves all assets)
        var sortedAssets = new List<AssetResponseDto>();
        sortedAssets.AddRange(datedAssets);
        sortedAssets.AddRange(undatedAssets);
        
        // Create consecutive chronological sets
        return CreateSetsFromList(sortedAssets, setSize);
    }
    
    /// <summary>
    /// Creates consecutive sets of assets from a pre-ordered list.
    /// Each set contains up to the specified number of assets in their original order.
    /// </summary>
    /// <param name="assets">The ordered list of assets to group into sets.</param>
    /// <param name="setSize">The maximum number of assets per set.</param>
    /// <returns>An enumerable collection of asset sets.</returns>
    private IEnumerable<IEnumerable<AssetResponseDto>> CreateSetsFromList(List<AssetResponseDto> assets, int setSize)
    {
        var sets = new List<List<AssetResponseDto>>();
        
        for (int i = 0; i < assets.Count; i += setSize)
        {
            var set = assets.Skip(i).Take(setSize).ToList();
            if (set.Any())
            {
                sets.Add(set);
            }
        }
        
        return sets;
    }

    /// <summary>
    /// Safely parses DateTime from an asset's EXIF information.
    /// Handles both the potentially incorrectly deserialized DateTime and direct JSON string parsing.
    /// </summary>
    /// <param name="asset">The asset to parse the date from.</param>
    /// <returns>The parsed DateTime if successful, null otherwise.</returns>
    private DateTime? TryParseDateTime(AssetResponseDto asset)
    {
        if (asset?.ExifInfo == null)
            return null;

        // Try to use the deserialized DateTimeOffset if it seems reasonable (after 1950 and before 2050)
        if (asset.ExifInfo.DateTimeOriginal.HasValue)
        {
            var deserializedDate = asset.ExifInfo.DateTimeOriginal.Value;
            if (deserializedDate.Year > 1950 && deserializedDate.Year <= 2050)
            {
                return deserializedDate.DateTime; // Convert DateTimeOffset to DateTime
            }
        }

        // If deserialized date is unreasonable, try to parse from filename patterns
        if (!string.IsNullOrEmpty(asset.OriginalFileName))
        {
            // Try to extract date from common filename patterns like "131005_140838.jpg" -> 2013-10-05
            var fileName = asset.OriginalFileName;
            
            // Pattern: YYMMDD_HHMMSS.ext (like 131005_140838.jpg)
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{6})_(\d{6})");
            if (match.Success)
            {
                var dateStr = match.Groups[1].Value; // YYMMDD
                var timeStr = match.Groups[2].Value; // HHMMSS
                
                if (dateStr.Length == 6 && timeStr.Length == 6)
                {
                    var year = 2000 + int.Parse(dateStr.Substring(0, 2)); // YY -> 20YY
                    var month = int.Parse(dateStr.Substring(2, 2));
                    var day = int.Parse(dateStr.Substring(4, 2));
                    var hour = int.Parse(timeStr.Substring(0, 2));
                    var minute = int.Parse(timeStr.Substring(2, 2));
                    var second = int.Parse(timeStr.Substring(4, 2));
                    
                    try
                    {
                        return new DateTime(year, month, day, hour, minute, second);
                    }
                    catch
                    {
                        // Invalid date components
                    }
                }
            }
        }

        // Fallback: return null for unparseable dates
        return null;
    }
}