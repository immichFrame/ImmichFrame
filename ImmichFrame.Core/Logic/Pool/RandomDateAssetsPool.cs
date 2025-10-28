using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

/// <summary>
/// Represents a photo cluster for balanced temporal selection.
/// Clusters group photos by time periods with similar photo density to ensure 
/// fair representation across different eras of photo collection.
/// </summary>
public class PhotoCluster
{
    /// <summary>
    /// The start date of this cluster's time range
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The end date of this cluster's time range
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// The number of photos in this cluster
    /// </summary>
    public int PhotoCount { get; set; }

    /// <summary>
    /// Statistical weight used for balanced random selection
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Indicates if this cluster has sparse photo density requiring special handling
    /// </summary>
    public bool IsSparse { get; set; }

    /// <summary>
    /// Returns a human-readable representation of the cluster
    /// </summary>
    public override string ToString()
    {
        var sparseIndicator = IsSparse ? " [SPARSE]" : "";
        return $"Cluster: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} ({PhotoCount} photos, weight: {Weight:F2}){sparseIndicator}";
    }
}

/// <summary>
/// Random date-based asset pool that provides balanced photo selection.
/// This pool creates clusters based on actual photo distribution to prevent 
/// over-representation of older photos in libraries with varying photo density over time.
/// Modern digital photography creates more photos than older analog photography,
/// so uniform random date selection would favor older, sparser periods.
/// </summary>
public class RandomDateAssetsPool : IAssetPool
{
    private readonly IApiCache apiCache;
    private readonly ImmichApi immichApi;
    private readonly IAccountSettings accountSettings;

    /// <summary>
    /// Initializes a new instance of RandomDateAssetsPool.
    /// </summary>
    /// <param name="apiCache">API cache for caching expensive operations</param>
    /// <param name="immichApi">Immich API client</param>
    /// <param name="accountSettings">Account-specific settings</param>
    /// <param name="enableClusterCaching">Enable cluster caching for production (default: true)</param>
    public RandomDateAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings, bool enableClusterCaching = true)
    {
        this.apiCache = apiCache;
        this.immichApi = immichApi;
        this.accountSettings = accountSettings;
        _enableClusterCaching = enableClusterCaching;
    }
    // Random number generator for date and asset selection - thread-safe in modern .NET
    private readonly Random _random = Random.Shared;
    
    // Maximum number of retry attempts for finding assets on random dates
    private const int MaxRetryAttempts = 4;

    // Default value for the number of assets to retrieve per request
    private int _requestedAssetCount = 50;

    // Configurable number of assets per random date selection
    // Higher values reduce API calls but may concentrate selection around fewer dates
    private int _assetsPerRandomDate = 10;

    // Photo clusters for balanced selection - initialized once per instance
    private List<PhotoCluster>? _photoClusters;
    
    // Flag to track whether clusters have been initialized to avoid recomputation
    private bool _clustersInitialized = false;

    // Track already-selected asset IDs to prevent duplicates
    private readonly HashSet<string> _selectedAssetIds = new();

    // Threshold for identifying sparse clusters (photos per month)
    private const int SparseClusterThreshold = 5;

    // Maximum range for cluster initialization to prevent excessive API calls
    private const int MaxClusterRangeYears = 30;

    // Cache key prefix for cluster data
    private const string ClusterCachePrefix = "RandomDatePool:Clusters";

    // Flag to enable cluster caching (disabled for testing to avoid interference)
    private readonly bool _enableClusterCaching;

    /// <summary>
    /// Gets the total asset count for statistics purposes.
    /// For random pools, exact asset count is not meaningful as it's dynamic,
    /// so we use the same logic as AllAssetsPool for statistics.
    /// This method leverages caching to avoid repeated API calls.
    /// </summary>
    /// <param name="ct">Cancellation token for the asynchronous operation</param>
    /// <returns>Total number of images in the account</returns>
    public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        var cacheKey = $"{nameof(RandomDateAssetsPool)}:stats:v1:archived={accountSettings.ShowArchived}";
        return (await apiCache.GetOrAddAsync(cacheKey,
            () => immichApi.GetAssetStatisticsAsync(null, accountSettings.ShowArchived, null, ct))).Images;
    }

    /// <summary>
    /// Retrieves the requested number of assets using cluster-based random selection.
    /// This is the main entry point for the asset pool, coordinating the entire
    /// selection process from cluster initialization to final asset filtering.
    /// </summary>
    /// <param name="requested">Number of assets to retrieve</param>
    /// <param name="ct">Cancellation token for the asynchronous operation</param>
    /// <returns>Collection of randomly selected assets</returns>
    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
        // Clear previous selection to ensure fresh results
        _selectedAssetIds.Clear();
        
        // Set the desired count for the LoadAssets method
        _requestedAssetCount = requested;
        var result = (await LoadAssets(ct)).Take(requested);
        return result;
    }

    /// <summary>
    /// Configures the number of assets to load per random date.
    /// This setting affects how many assets are retrieved for each randomly selected date
    /// before moving to the next date in the selection algorithm.
    /// Higher values improve efficiency but may reduce date diversity.
    /// </summary>
    /// <param name="assetsPerDate">Number of assets per date (minimum: 1)</param>
    public void ConfigureAssetsPerRandomDate(int assetsPerDate)
    {
        _assetsPerRandomDate = Math.Max(1, assetsPerDate); // Ensure minimum of 1
    }

    /// <summary>
    /// Main asset loading method that orchestrates the cluster-based selection process.
    /// This method applies account-specific filters after retrieving assets from the
    /// cluster-based random selection algorithm.
    /// </summary>
    /// <param name="ct">Cancellation token for the asynchronous operation</param>
    /// <returns>Filtered collection of assets ready for display</returns>
    private async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var assets = await LoadAssetsInternal(ct);
        return ApplyAccountFilters(assets);
    }

    /// <summary>
    /// Applies account-specific filters to the asset collection.
    /// Filters include image type, archive status, date ranges, and rating constraints.
    /// This ensures only assets matching the user's preferences are returned.
    /// </summary>
    /// <param name="assets">Raw asset collection to filter</param>
    /// <returns>Filtered asset collection based on account settings</returns>
    private IEnumerable<AssetResponseDto> ApplyAccountFilters(IEnumerable<AssetResponseDto> assets)
    {
        // Display only Images (not videos) - this pool is specifically for photo selection
        var filteredAssets = assets.Where(x => x.Type == AssetTypeEnum.IMAGE);

        // Filter out archived assets if not explicitly requested by user settings
        if (!accountSettings.ShowArchived)
            filteredAssets = filteredAssets.Where(x => x.IsArchived == false);

        // Apply upper date boundary if specified in account settings
        var takenBefore = accountSettings.ImagesUntilDate.HasValue ? accountSettings.ImagesUntilDate : null;
        if (takenBefore.HasValue)
        {
            filteredAssets = filteredAssets.Where(x =>
                (x.ExifInfo?.DateTimeOriginal?.DateTime ?? x.FileCreatedAt.DateTime) <= takenBefore.Value);
        }

        // Apply lower date boundary (either absolute date or relative days from today)
        var takenAfter = accountSettings.ImagesFromDate.HasValue
            ? accountSettings.ImagesFromDate
            : accountSettings.ImagesFromDays.HasValue
                ? DateTime.Today.AddDays(-accountSettings.ImagesFromDays.Value)
                : (DateTime?)null;
        if (takenAfter.HasValue)
        {
            filteredAssets = filteredAssets.Where(x =>
                (x.ExifInfo?.DateTimeOriginal?.DateTime ?? x.FileCreatedAt.DateTime) >= takenAfter.Value);
        }

        // Apply rating filter if specified - only show assets with specific star rating
        if (accountSettings.Rating is int rating)
        {
            filteredAssets = filteredAssets.Where(x => x.ExifInfo != null && x.ExifInfo.Rating == rating);
        }

        return filteredAssets;
    }

    /// <summary>
    /// Core asset loading logic using cluster-based random selection.
    /// This method determines the date range, initializes photo clusters, and executes 
    /// the cluster-based selection algorithm. It represents the heart of the balanced
    /// photo selection system that prevents over-representation of older photos.
    /// </summary>
    /// <param name="ct">Cancellation token for the asynchronous operation</param>
    /// <returns>Collection of assets selected using cluster-based algorithm</returns>
    protected async Task<IEnumerable<AssetResponseDto>> LoadAssetsInternal(CancellationToken ct = default)
    {
        // First, find the oldest and youngest assets to determine the available date range
        // This establishes the temporal boundaries for cluster creation
        var (oldestAsset, youngestAsset) = await GetOldestAndYoungestAssetsAsync(ct);

        if (oldestAsset == null || youngestAsset == null)
        {
            return Enumerable.Empty<AssetResponseDto>();
        }

        // Extract actual dates from EXIF data (taken date) or fallback to file creation date
        var oldestDate = GetAssetDate(oldestAsset);
        var youngestDate = GetAssetDate(youngestAsset);

        if (oldestDate >= youngestDate)
        {
            // If dates are the same or invalid, return all available assets as fallback
            return await GetAllAvailableAssets(ct);
        }

        // Initialize photo clusters on first use for balanced selection
        await InitializePhotoClusters(oldestDate, youngestDate, ct);

        // Execute cluster-based random date selection with escalating time ranges
        var assets = await TryGetAssetsFromClusterBasedRandomDates(ct);

        return assets ?? Enumerable.Empty<AssetResponseDto>();
    }

    /// <summary>
    /// Retrieves the oldest and youngest assets from the photo library to establish the date range.
    /// This method queries the Immich API to find the temporal boundaries of the available photos,
    /// which is essential for cluster-based balanced selection. It uses separate queries for 
    /// optimal performance and handles edge cases gracefully.
    /// </summary>
    /// <param name="ct">Cancellation token for the asynchronous operation</param>
    /// <returns>Tuple containing the oldest and youngest assets, or (null, null) if no assets found</returns>
    private async Task<(AssetResponseDto? oldest, AssetResponseDto? youngest)> GetOldestAndYoungestAssetsAsync(CancellationToken ct)
    {
        try
        {
            // Query for the oldest asset (ascending order, first result)
            var oldestSearch = new MetadataSearchDto
            {
                Size = 1,
                Page = 1,
                Order = AssetOrder.Asc, // Oldest first
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline
            };

            var oldestResult = await immichApi.SearchAssetsAsync(oldestSearch, ct);
            var oldestAsset = oldestResult?.Assets?.Items?.FirstOrDefault();

            // Query for the youngest asset (descending order, first result)
            var youngestSearch = new MetadataSearchDto
            {
                Size = 1,
                Page = 1,
                Order = AssetOrder.Desc, // Newest first
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline
            };

            var youngestResult = await immichApi.SearchAssetsAsync(youngestSearch, ct);
            var youngestAsset = youngestResult?.Assets?.Items?.FirstOrDefault();

            return (oldestAsset, youngestAsset);
        }
        catch
        {
            // Return null values if any error occurs during the queries
            // TODO: Log exception for troubleshooting waiting for a global error handling solution
            return (null, null);
        }
    }

    /// <summary>
    /// Extracts the most accurate date from an asset using multiple fallback strategies.
    /// Priority order: 1) EXIF taken date (most accurate), 2) File creation date as fallback.
    /// This ensures proper temporal ordering for cluster-based selection even with missing EXIF data.
    /// The method handles edge cases where EXIF data might be corrupted or missing.
    /// </summary>
    /// <param name="asset">Asset to extract date from</param>
    /// <returns>The most reliable date available for the asset</returns>
    private DateTime GetAssetDate(AssetResponseDto asset)
    {
        // Prefer EXIF taken date as it represents when the photo was actually captured
        if (asset.ExifInfo?.DateTimeOriginal != null)
        {
            return asset.ExifInfo.DateTimeOriginal.Value.DateTime;
        }

        // Fallback to file creation date if EXIF data is unavailable
        return asset.FileCreatedAt.DateTime;
    }

    /// <summary>
    /// Advanced cluster-based random date selection method (preferred approach).
    /// This method uses photo clusters to ensure balanced representation across different
    /// time periods, preventing over-representation of older photos in libraries with
    /// varying photo density. Each cluster represents a time period with similar photo count.
    /// The algorithm cycles through clusters and time ranges to maximize photo diversity.
    /// </summary>
    /// <param name="ct">Cancellation token for the asynchronous operation</param>
    /// <returns>List of assets selected using cluster-based balanced approach</returns>
    private async Task<List<AssetResponseDto>?> TryGetAssetsFromClusterBasedRandomDates(CancellationToken ct)
    {
        if (_photoClusters == null || !_photoClusters.Any())
        {
            return null;
        }

        var allAssets = new List<AssetResponseDto>();

        // Calculate how many different random dates are needed based on assets per date
        var requiredDateBlocks = Math.Ceiling((double)_requestedAssetCount / _assetsPerRandomDate);
        // Scale attempts: 4x date blocks (to cycle through time ranges), capped at request/2, minimum 12
        var maxDateAttempts = Math.Min((int)requiredDateBlocks * 4, Math.Max(12, _requestedAssetCount / 2));
        PhotoCluster? currentCluster = null;
        DateTime? currentRandomDate = null;

        for (int attempt = 0; attempt < maxDateAttempts && allAssets.Count < _requestedAssetCount; attempt++)
        {
            // Generate new random date from a new cluster only on attempts 0, 4, 8, 12, etc. (every 4 attempts)
            // This allows cycling through different time ranges for each cluster-selected date
            if (attempt % 4 == 0)
            {
                currentCluster = SelectRandomCluster();
                currentRandomDate = GenerateRandomDateFromCluster(currentCluster);
                // Cluster tracking removed as it was unused
            }
            
            if (currentRandomDate == null || currentCluster == null)
            {
                continue;
            }

            // Determine the time range based on the current attempt (1-4 cycle)
            // This creates escalating search windows: ±7 days, ±3 months, ±6 months, ±12 months
            // For sparse clusters, use wider, non-overlapping windows
            var timeRangeAttempt = (attempt % 4) + 1;
            var (searchStartDate, searchEndDate, description) = GetSearchTimeRange(currentRandomDate.Value, timeRangeAttempt, currentCluster.IsSparse);

            var assets = await SearchAssetsInTimeRange(searchStartDate, searchEndDate, ct);

            if (assets.Any())
            {
                allAssets.AddRange(assets);
            }

            // Stop if we have enough assets
            if (allAssets.Count >= _requestedAssetCount)
            {
                break;
            }
        }

        // If we have accumulated sufficient assets, return them; otherwise use fallback
        if (allAssets.Count >= Math.Min(_assetsPerRandomDate, _requestedAssetCount))
        {
            return allAssets;
        }

        // If we don't have enough assets, try fallback approach
        var fallbackAssets = await GetAllAvailableAssets(ct);
        var result = fallbackAssets.Take(Math.Max(100, _requestedAssetCount)).ToList();
        return result;
    }

    /// <summary>
    /// Generates a random date within the specified cluster's time range.
    /// This method ensures even distribution within the cluster boundaries,
    /// taking advantage of the cluster's balanced photo density to prevent
    /// over-representation of any particular time period within the cluster.
    /// </summary>
    /// <param name="cluster">Photo cluster to generate date from</param>
    /// <returns>Random date within the cluster's time range</returns>
    private DateTime GenerateRandomDateFromCluster(PhotoCluster cluster)
    {
        // Generate a random date within the cluster time range
        var totalDays = (cluster.EndDate - cluster.StartDate).TotalDays;
        var randomDays = _random.NextDouble() * totalDays;
        var randomDate = cluster.StartDate.AddDays(randomDays);

        return randomDate.Date;
    }

    /// <summary>
    /// Queries assets within the provided time range and returns a randomized subset.
    /// Implements deduplication to prevent returning the same assets multiple times.
    /// </summary>
    /// <param name="startDate">Inclusive start of the range</param>
    /// <param name="endDate">Exclusive end of the range</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Randomized list of up to <c>_assetsPerRandomDate</c> assets</returns>
    private async Task<List<AssetResponseDto>> SearchAssetsInTimeRange(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        try
        {
            var searchDto = new MetadataSearchDto
            {
                TakenAfter = startDate,
                TakenBefore = endDate,
                Size = Math.Max(_assetsPerRandomDate * 4, 100), // Request more to account for deduplication
                Page = 1,
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline,
                Order = AssetOrder.Desc
            };

            // Query the API for assets in the requested range
            var result = await immichApi.SearchAssetsAsync(searchDto, ct);
            var assets = result?.Assets?.Items?.ToList() ?? new List<AssetResponseDto>();

            // Filter out already-selected assets to prevent duplicates
            var newAssets = assets.Where(asset => !_selectedAssetIds.Contains(asset.Id)).ToList();

            // Shuffle to avoid bias from API ordering and take only what's needed
            var selectedAssets = Shuffle(newAssets, Random.Shared).Take(_assetsPerRandomDate).ToList();

            // Track the selected assets to prevent future duplicates
            foreach (var asset in selectedAssets)
            {
                _selectedAssetIds.Add(asset.Id);
            }

            return selectedAssets;
        }
        catch (Exception)
        {
            // TODO: Waiting for a global log error Handling...
            return new List<AssetResponseDto>();
        }
    }

    /// <summary>
    /// Generates a random unique date within the given range, avoiding duplicates.
    /// </summary>
    /// <param name="oldestDate">Range start</param>
    /// <param name="youngestDate">Range end</param>
    /// <param name="attemptedDates">Dates already tried in this session</param>
    /// <returns>A unique random date or null when exhaustion is reached</returns>
    private DateTime? GenerateRandomDate(DateTime oldestDate, DateTime youngestDate, HashSet<DateTime> attemptedDates)
    {
        // Cap attempts to prevent infinite loops in very small ranges
        const int maxAttempts = 20;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            var totalDays = (youngestDate - oldestDate).TotalDays;
            var randomDays = _random.NextDouble() * totalDays;
            var randomDate = oldestDate.AddDays(randomDays).Date;

            if (!attemptedDates.Contains(randomDate))
            {
                return randomDate;
            }

            attempts++;
        }

        return null;
    }

    /// <summary>
    /// Computes an escalating search window around a target date.
    /// Uses adaptive windowing: non-overlapping sequential windows for sparse clusters,
    /// traditional escalating windows for dense clusters.
    /// </summary>
    /// <param name="targetDate">Center of the search</param>
    /// <param name="attemptNumber">Attempt index that maps to window size</param>
    /// <param name="isSparseCluster">Whether the current cluster is sparse</param>
    /// <returns>Tuple of start, end and a human-readable description</returns>
    private (DateTime searchStart, DateTime searchEnd, string description) GetSearchTimeRange(DateTime targetDate, int attemptNumber, bool isSparseCluster = false)
    {
        DateTime searchStart, searchEnd;
        string description;

        if (isSparseCluster)
        {
            // For sparse clusters: use larger, non-overlapping sequential windows
            switch (attemptNumber)
            {
                case 1:
                    // Wide: ±6 months
                    searchStart = targetDate.AddMonths(-6);
                    searchEnd = targetDate.AddMonths(6);
                    description = "6-month range (sparse)";
                    break;
                case 2:
                    // Very wide: ±12 months
                    searchStart = targetDate.AddMonths(-12);
                    searchEnd = targetDate.AddMonths(12);
                    description = "12-month range (sparse)";
                    break;
                case 3:
                    // Extremely wide: ±18 months
                    searchStart = targetDate.AddMonths(-18);
                    searchEnd = targetDate.AddMonths(18);
                    description = "18-month range (sparse)";
                    break;
                default:
                    // Maximum: ±24 months
                    searchStart = targetDate.AddMonths(-24);
                    searchEnd = targetDate.AddMonths(24);
                    description = "24-month range (sparse)";
                    break;
            }
        }
        else
        {
            // For dense clusters: use traditional escalating windows
            switch (attemptNumber)
            {
                case 1:
                    // Narrow: ±7 days
                    searchStart = targetDate.AddDays(-7);
                    searchEnd = targetDate.AddDays(7);
                    description = "7-day range";
                    break;
                case 2:
                    // Medium: ±3 months
                    searchStart = targetDate.AddMonths(-3);
                    searchEnd = targetDate.AddMonths(3);
                    description = "3-month range";
                    break;
                case 3:
                    // Wide: ±6 months
                    searchStart = targetDate.AddMonths(-6);
                    searchEnd = targetDate.AddMonths(6);
                    description = "6-month range";
                    break;
                default:
                    // Widest: ±12 months
                    searchStart = targetDate.AddMonths(-12);
                    searchEnd = targetDate.AddMonths(12);
                    description = "12-month range";
                    break;
            }
        }

        return (searchStart, searchEnd, description);
    }

    /// <summary>
    /// Broad fallback query across all images, used when date-scoped attempts fail.
    /// Implements deduplication to prevent returning already-selected assets.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Randomized list of assets from entire collection</returns>
    private async Task<List<AssetResponseDto>> GetAllAvailableAssets(CancellationToken ct)
    {
        try
        {
            var searchDto = new MetadataSearchDto
            {
                Size = Math.Max(400, _requestedAssetCount * 6), // Request more to account for deduplication
                Page = 1,
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline,
                Order = AssetOrder.Desc
            };

            // Query the API and then randomize to provide visual variety
            var result = await immichApi.SearchAssetsAsync(searchDto, ct);
            var assets = result?.Assets?.Items?.ToList() ?? new List<AssetResponseDto>();

            // Filter out already-selected assets to prevent duplicates
            var newAssets = assets.Where(asset => !_selectedAssetIds.Contains(asset.Id)).ToList();

            // Randomize and track selected assets
            var selectedAssets = Shuffle(newAssets, Random.Shared);
            
            // Track the selected assets (up to requested amount)
            foreach (var asset in selectedAssets.Take(_requestedAssetCount))
            {
                _selectedAssetIds.Add(asset.Id);
            }

            return selectedAssets;
        }
        catch
        {
            // TODO: Waiting for a global log error Handling...
            return new List<AssetResponseDto>();
        }
    }

    /// <summary>
    /// Initializes the temporal clusters used for balanced selection.
    /// Implements optional caching and range bounding to prevent expensive wide-range initialization.
    /// </summary>
    /// <param name="oldestDate">Library start</param>
    /// <param name="youngestDate">Library end</param>
    /// <param name="ct">Cancellation token</param>
    private async Task InitializePhotoClusters(DateTime oldestDate, DateTime youngestDate, CancellationToken ct)
    {
        if (_clustersInitialized && _photoClusters != null)
            return;

        try
        {
            // Bound the date range to prevent excessive API calls for very long libraries
            var boundedOldestDate = BoundDateRange(oldestDate, youngestDate);
            
            // Use caching for large date ranges in production environments
            var rangeYears = (youngestDate - boundedOldestDate).TotalDays / 365.25;
            var shouldCache = _enableClusterCaching && rangeYears > 2; // Cache for ranges > 2 years
            
            if (shouldCache)
            {
                // Generate cache key for this specific date range and account settings
                var cacheKey = GenerateClusterCacheKey(boundedOldestDate, youngestDate);
                
                // Try to get cached cluster data first
                _photoClusters = await apiCache.GetOrAddAsync(cacheKey, async () =>
                {
                    // 1) Gather monthly photo counts across the bounded range
                    var monthlyStats = await GetMonthlyPhotoStatistics(boundedOldestDate, youngestDate, ct);
                    
                    // 2) Convert monthly stats into balanced clusters
                    return CreateBalancedClusters(monthlyStats);
                });
            }
            else
            {
                // For smaller ranges or when caching is disabled, compute directly
                var monthlyStats = await GetMonthlyPhotoStatistics(boundedOldestDate, youngestDate, ct);
                _photoClusters = CreateBalancedClusters(monthlyStats);
            }
            
            _clustersInitialized = true;
        }
        catch (Exception)
        {
            // Fallback to a single catch-all cluster to keep the app functional
            _photoClusters = new List<PhotoCluster>
            {
                new PhotoCluster
                {
                    StartDate = oldestDate,
                    EndDate = youngestDate,
                    PhotoCount = 1000,
                    Weight = 1.0,
                    IsSparse = false
                }
            };
            _clustersInitialized = true;
        }
    }

    /// <summary>
    /// Produces monthly photo counts between the given dates.
    /// </summary>
    /// <param name="oldestDate">Start date (inclusive)</param>
    /// <param name="youngestDate">End date (inclusive)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of (Month, PhotoCount) tuples for each month in range</returns>
    private async Task<List<(DateTime Month, int PhotoCount)>> GetMonthlyPhotoStatistics(DateTime oldestDate, DateTime youngestDate, CancellationToken ct)
    {
        var monthlyStats = new List<(DateTime Month, int PhotoCount)>();
        var currentDate = new DateTime(oldestDate.Year, oldestDate.Month, 1);
        var endDate = new DateTime(youngestDate.Year, youngestDate.Month, 1);

        while (currentDate <= endDate)
        {
            var monthStart = currentDate;
            var monthEnd = currentDate.AddMonths(1).AddDays(-1);

            // Clamp month to library boundaries
            if (monthStart < oldestDate) monthStart = oldestDate;
            if (monthEnd > youngestDate) monthEnd = youngestDate;

            try
            {
                var searchDto = new MetadataSearchDto
                {
                    Size = 1,
                    Page = 1,
                    Type = AssetTypeEnum.IMAGE,
                    TakenAfter = monthStart,
                    TakenBefore = monthEnd.AddDays(1),
                    WithExif = true,
                    Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline
                };

                // Use the API's Total field to infer monthly counts cheaply
                var result = await immichApi.SearchAssetsAsync(searchDto, ct);
                var photoCount = result?.Assets?.Total ?? 0;

                monthlyStats.Add((currentDate, photoCount));
            }
            catch
            {
                // TODO: Waiting for a global log error Handling...
                monthlyStats.Add((currentDate, 0));
            }

            currentDate = currentDate.AddMonths(1);
        }

        return monthlyStats;
    }

    /// <summary>
    /// Converts monthly counts into clusters that each represent roughly equal photo counts.
    /// </summary>
    /// <param name="monthlyStats">Per-month photo counts</param>
    /// <returns>List of clusters with weights assigned for balanced selection</returns>
    private List<PhotoCluster> CreateBalancedClusters(List<(DateTime Month, int PhotoCount)> monthlyStats)
    {
        var clusters = new List<PhotoCluster>();
        var totalPhotos = monthlyStats.Sum(x => x.PhotoCount);

        if (totalPhotos == 0)
        {
            return new List<PhotoCluster>
            {
                new PhotoCluster
                {
                    StartDate = monthlyStats.First().Month,
                    EndDate = monthlyStats.Last().Month.AddMonths(1).AddDays(-1),
                    PhotoCount = 0,
                    Weight = 1.0
                }
            };
        }

        const int targetClusters = 10;
        var targetPhotosPerCluster = Math.Max(1, totalPhotos / targetClusters);

        var currentCluster = new PhotoCluster();
        var currentPhotoCount = 0;
        bool firstMonth = true;

        for (int i = 0; i < monthlyStats.Count; i++)
        {
            var (month, photoCount) = monthlyStats[i];

            if (firstMonth)
            {
                currentCluster.StartDate = month;
                firstMonth = false;
            }

            currentPhotoCount += photoCount;
            currentCluster.EndDate = month.AddMonths(1).AddDays(-1);

            var isLastMonth = i == monthlyStats.Count - 1;

            if (currentPhotoCount >= targetPhotosPerCluster && !isLastMonth)
            {
                currentCluster.PhotoCount = currentPhotoCount;
                clusters.Add(currentCluster);

                currentCluster = new PhotoCluster { StartDate = month.AddMonths(1) };
                currentPhotoCount = 0;
            }
        }

        if (currentPhotoCount > 0)
        {
            currentCluster.PhotoCount = currentPhotoCount;
            clusters.Add(currentCluster);
        }

        // Assign equal weights to clusters to balance selection probability across eras
        foreach (var cluster in clusters)
        {
            cluster.Weight = 1.0 / clusters.Count;
            
            // Mark clusters as sparse if they have very few photos relative to their time span
            var clusterDurationMonths = Math.Max(1, (cluster.EndDate - cluster.StartDate).Days / 30.0);
            var photosPerMonth = cluster.PhotoCount / clusterDurationMonths;
            cluster.IsSparse = photosPerMonth < SparseClusterThreshold;
        }

        return clusters;
    }

    /// <summary>
    /// Selects a cluster based on cumulative weights (roulette wheel selection).
    /// </summary>
    /// <returns>Randomly selected cluster</returns>
    /// <exception cref="InvalidOperationException">If clusters are not initialized</exception>
    private PhotoCluster SelectRandomCluster()
    {
        if (_photoClusters == null || !_photoClusters.Any())
        {
            throw new InvalidOperationException("Clusters are not initialized.");
        }

        // Draw once and walk the cumulative distribution
        var randomValue = _random.NextDouble();
        var cumulativeWeight = 0.0;

        foreach (var cluster in _photoClusters)
        {
            cumulativeWeight += cluster.Weight;
            if (randomValue <= cumulativeWeight)
            {
                return cluster;
            }
        }

        return _photoClusters.Last();
    }

    /// <summary>
    /// Efficiently shuffles a list using Fisher-Yates algorithm.
    /// More efficient than OrderBy(_ => random.Next()) which is O(n log n).
    /// </summary>
    /// <typeparam name="T">Type of list elements</typeparam>
    /// <param name="list">List to shuffle</param>
    /// <param name="random">Random number generator</param>
    /// <returns>The same list, shuffled in-place</returns>
    private static List<T> Shuffle<T>(List<T> list, Random random)
    {
        var n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    /// <summary>
    /// Bounds the date range to prevent excessive cluster initialization for very long libraries.
    /// Limits the oldest date to MaxClusterRangeYears from the youngest date.
    /// </summary>
    /// <param name="oldestDate">Original oldest date</param>
    /// <param name="youngestDate">Youngest date (unchanged)</param>
    /// <returns>Bounded oldest date</returns>
    private DateTime BoundDateRange(DateTime oldestDate, DateTime youngestDate)
    {
        var maxOldestDate = youngestDate.AddYears(-MaxClusterRangeYears);
        return oldestDate < maxOldestDate ? maxOldestDate : oldestDate;
    }

    /// <summary>
    /// Generates a cache key for cluster data based on date range and account settings.
    /// </summary>
    /// <param name="oldestDate">Bounded oldest date</param>
    /// <param name="youngestDate">Youngest date</param>
    /// <returns>Cache key for cluster data</returns>
    private string GenerateClusterCacheKey(DateTime oldestDate, DateTime youngestDate)
    {
        return $"{ClusterCachePrefix}:v1:range={oldestDate:yyyy-MM}to{youngestDate:yyyy-MM}:archived={accountSettings.ShowArchived}";
    }
}