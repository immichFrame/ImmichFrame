using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic.Pool;

public class PhotoCluster
{
                public DateTime StartDate { get; set; }
    
                public DateTime EndDate { get; set; }
    
                public int PhotoCount { get; set; }
    
                public double Weight { get; set; }
    
    public override string ToString()
    {
        return $"Cluster: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} ({PhotoCount} photos, weight: {Weight:F2})";
    }
}

public class RandomDateAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings) : IAssetPool
{
    private readonly Random _random = new();
    private const int MaxRetryAttempts = 4;
    
        private int _requestedAssetCount = 50; 
    
        private int _assetsPerRandomDate = 10; 
    
        private List<PhotoCluster>? _photoClusters;
    private bool _clustersInitialized = false;

                        public async Task<long> GetAssetCount(CancellationToken ct = default)
    {
        return (await apiCache.GetOrAddAsync(nameof(RandomDateAssetsPool),
            () => immichApi.GetAssetStatisticsAsync(null, false, null, ct))).Images;
    }

                            public async Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, CancellationToken ct = default)
    {
                _requestedAssetCount = requested;
        var result = (await LoadAssets(ct)).Take(requested);
        return result;
    }

                            public void ConfigureAssetsPerRandomDate(int assetsPerDate)
    {
        _assetsPerRandomDate = Math.Max(1, assetsPerDate);     }

                            private async Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
    {
        var assets = await LoadAssetsInternal(ct);
        return ApplyAccountFilters(assets);
    }

                            private IEnumerable<AssetResponseDto> ApplyAccountFilters(IEnumerable<AssetResponseDto> assets)
    {
                var filteredAssets = assets.Where(x => x.Type == AssetTypeEnum.IMAGE);

                if (!accountSettings.ShowArchived)
            filteredAssets = filteredAssets.Where(x => x.IsArchived == false);

                var takenBefore = accountSettings.ImagesUntilDate.HasValue ? accountSettings.ImagesUntilDate : null;
        if (takenBefore.HasValue)
        {
            filteredAssets = filteredAssets.Where(x => x.ExifInfo.DateTimeOriginal <= takenBefore);
        }

                var takenAfter = accountSettings.ImagesFromDate.HasValue ? accountSettings.ImagesFromDate : accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-accountSettings.ImagesFromDays.Value) : null;
        if (takenAfter.HasValue)
        {
            filteredAssets = filteredAssets.Where(x => x.ExifInfo.DateTimeOriginal >= takenAfter);
        }

                if (accountSettings.Rating is int rating)
        {
            filteredAssets = filteredAssets.Where(x => x.ExifInfo.Rating == rating);
        }
        
        return filteredAssets;
    }

                            protected async Task<IEnumerable<AssetResponseDto>> LoadAssetsInternal(CancellationToken ct = default)
    {
                var (oldestAsset, youngestAsset) = await GetOldestAndYoungestAssetsAsync(ct);

        if (oldestAsset == null || youngestAsset == null)
        {
            return Enumerable.Empty<AssetResponseDto>();
        }

                var oldestDate = GetAssetDate(oldestAsset);
        var youngestDate = GetAssetDate(youngestAsset);

        if (oldestDate >= youngestDate)
        {
                        return await GetAllAvailableAssets(ct);
        }

                await InitializePhotoClusters(oldestDate, youngestDate, ct);

                var assets = await TryGetAssetsFromClusterBasedRandomDates(ct);

        return assets ?? Enumerable.Empty<AssetResponseDto>();
    }

                                                        private async Task<(AssetResponseDto? oldest, AssetResponseDto? youngest)> GetOldestAndYoungestAssetsAsync(CancellationToken ct)
    {
        try
        {
                        var oldestSearch = new MetadataSearchDto
            {
                Size = 1,
                Page = 1,
                Order = AssetOrder.Asc,
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline
            };

            var oldestResult = await immichApi.SearchAssetsAsync(oldestSearch, ct);
            var oldestAsset = oldestResult?.Assets?.Items?.FirstOrDefault();

                        var youngestSearch = new MetadataSearchDto
            {
                Size = 1,
                Page = 1,
                Order = AssetOrder.Desc,
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
                        return (null, null);
        }
    }

                                                        private DateTime GetAssetDate(AssetResponseDto asset)
    {
                if (asset.ExifInfo?.DateTimeOriginal != null)
        {
            return asset.ExifInfo.DateTimeOriginal.Value.DateTime;
        }
        
        return asset.FileCreatedAt.DateTime;
    }

                                        private async Task<List<AssetResponseDto>?> TryGetAssetsFromRandomDates(DateTime oldestDate, DateTime youngestDate, CancellationToken ct)
    {
        var allAssets = new List<AssetResponseDto>();
        var attemptedDates = new HashSet<DateTime>();
        
                var requiredDateBlocks = Math.Ceiling((double)_requestedAssetCount / _assetsPerRandomDate);
        var maxDateAttempts = Math.Min((int)requiredDateBlocks * 4, 12); 
        DateTime? currentRandomDate = null;
        
        for (int attempt = 0; attempt < maxDateAttempts && allAssets.Count < _requestedAssetCount; attempt++)
        {
                        if (attempt % 4 == 0)
            {
                currentRandomDate = GenerateRandomDate(oldestDate, youngestDate, attemptedDates);
                
                if (currentRandomDate == null)
                {
                                        break;
                }

                attemptedDates.Add(currentRandomDate.Value);
            }

            if (currentRandomDate == null)
            {
                break;
            }

                        var timeRangeAttempt = (attempt % 4) + 1;
            var searchDate = currentRandomDate.Value.Date;
            var assetsFromDate = await GetAssetsFromDate(searchDate, timeRangeAttempt, ct);
            
            if (assetsFromDate.Any())
            {
                                var assetsNeeded = Math.Min(_assetsPerRandomDate, _requestedAssetCount - allAssets.Count);
                var assetsToAdd = assetsFromDate.Take(assetsNeeded).ToList();
                allAssets.AddRange(assetsToAdd);
                
                                if (allAssets.Count >= _requestedAssetCount)
                {
                    return allAssets;
                }
            }

                                                            if (attempt > 0 && attempt % 8 == 0)
            {
                var oldYoungestDate = youngestDate;
                youngestDate = searchDate.AddDays(-30);                 if (youngestDate <= oldestDate)
                {
                    break;
                }
            }
        }

                if (allAssets.Count >= Math.Min(_assetsPerRandomDate, _requestedAssetCount))
        {
            return allAssets;
        }

                var fallbackAssets = await GetAllAvailableAssets(ct);
        var result = fallbackAssets.Take(Math.Max(100, _requestedAssetCount)).ToList();
        return result;
    }

    private async Task<List<AssetResponseDto>?> TryGetAssetsFromClusterBasedRandomDates(CancellationToken ct)
    {
        if (_photoClusters == null || !_photoClusters.Any())
        {
            return null;
        }

        var allAssets = new List<AssetResponseDto>();
        var usedClusters = new HashSet<PhotoCluster>();
        
                var requiredDateBlocks = Math.Ceiling((double)_requestedAssetCount / _assetsPerRandomDate);
        var maxDateAttempts = Math.Min((int)requiredDateBlocks * 4, 12); 
        PhotoCluster? currentCluster = null;
        DateTime? currentRandomDate = null;
        
        for (int attempt = 0; attempt < maxDateAttempts && allAssets.Count < _requestedAssetCount; attempt++)
        {
                        if (attempt % 4 == 0)
            {
                currentCluster = SelectRandomCluster();
                currentRandomDate = GenerateRandomDateFromCluster(currentCluster);
                usedClusters.Add(currentCluster);
            }
            if (currentRandomDate == null || currentCluster == null)
            {
                continue;
            }

                        var timeRangeAttempt = (attempt % 4) + 1;
            var (searchStartDate, searchEndDate, description) = GetSearchTimeRange(currentRandomDate.Value, timeRangeAttempt);

            var assets = await SearchAssetsInTimeRange(searchStartDate, searchEndDate, ct);
            
            if (assets.Any())
            {
                allAssets.AddRange(assets);
            }

                        if (allAssets.Count >= _requestedAssetCount)
            {
                break;
            }
        }

                if (allAssets.Count >= Math.Min(_assetsPerRandomDate, _requestedAssetCount))
        {
            return allAssets;
        }

                var fallbackAssets = await GetAllAvailableAssets(ct);
        var result = fallbackAssets.Take(Math.Max(100, _requestedAssetCount)).ToList();
        return result;
    }

    private DateTime GenerateRandomDateFromCluster(PhotoCluster cluster)
    {
                var totalDays = (cluster.EndDate - cluster.StartDate).TotalDays;
        var randomDays = _random.NextDouble() * totalDays;
        var randomDate = cluster.StartDate.AddDays(randomDays);
        
        return randomDate.Date;     }

    private async Task<List<AssetResponseDto>> SearchAssetsInTimeRange(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        try
        {
            var searchDto = new MetadataSearchDto
            {
                TakenAfter = startDate,
                TakenBefore = endDate,
                Size = Math.Max(_assetsPerRandomDate * 2, 50),                 Page = 1,
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline,
                Order = AssetOrder.Desc             };

            var result = await immichApi.SearchAssetsAsync(searchDto, ct);
            var assets = result?.Assets?.Items?.ToList() ?? new List<AssetResponseDto>();
            
                        return assets.OrderBy(_ => _random.Next()).Take(_assetsPerRandomDate).ToList();
        }
        catch (Exception)
        {
            return new List<AssetResponseDto>();
        }
    }

    private DateTime? GenerateRandomDate(DateTime oldestDate, DateTime youngestDate, HashSet<DateTime> attemptedDates)
    {
        const int maxAttempts = 20;         int attempts = 0;

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

        return null;     }

    private async Task<List<AssetResponseDto>> GetAssetsFromDate(DateTime targetDate, int attemptNumber, CancellationToken ct)
    {
        try
        {
                        var (searchStart, searchEnd, description) = GetSearchTimeRange(targetDate, attemptNumber);

            var searchDto = new MetadataSearchDto
            {
                TakenAfter = searchStart,
                TakenBefore = searchEnd,
                Size = Math.Max(_assetsPerRandomDate * 2, 50),                 Page = 1,
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline,
                Order = AssetOrder.Desc             };

            var result = await immichApi.SearchAssetsAsync(searchDto, ct);
            var assets = result?.Assets?.Items?.ToList() ?? new List<AssetResponseDto>();
            
            return assets;
        }
        catch
        {
            return new List<AssetResponseDto>();
        }
    }

    private (DateTime searchStart, DateTime searchEnd, string description) GetSearchTimeRange(DateTime targetDate, int attemptNumber)
    {
        DateTime searchStart, searchEnd;
        string description;

        switch (attemptNumber)
        {
            case 1:
                                searchStart = targetDate.AddDays(-7);
                searchEnd = targetDate.AddDays(7);
                description = "(±7 Tage)";
                break;
            case 2:
                                searchStart = targetDate.AddMonths(-3);
                searchEnd = targetDate.AddMonths(3);
                description = "(±3 Monate)";
                break;
            case 3:
                                searchStart = targetDate.AddMonths(-6);
                searchEnd = targetDate.AddMonths(6);
                description = "(±6 Monate)";
                break;
            default:
                                searchStart = targetDate.AddMonths(-12);
                searchEnd = targetDate.AddMonths(12);
                description = "(±12 Monate)";
                break;
        }

        return (searchStart, searchEnd, description);
    }

    private async Task<List<AssetResponseDto>> GetAllAvailableAssets(CancellationToken ct)
    {
        try
        {
                        var searchDto = new MetadataSearchDto
            {
                Size = Math.Max(200, _requestedAssetCount * 3),                 Page = 1,
                Type = AssetTypeEnum.IMAGE,
                WithExif = true,
                Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline,
                Order = AssetOrder.Desc
            };

            var result = await immichApi.SearchAssetsAsync(searchDto, ct);
            var assets = result?.Assets?.Items?.ToList() ?? new List<AssetResponseDto>();
            
                        return assets.OrderBy(_ => _random.Next()).ToList();
        }
        catch
        {
            return new List<AssetResponseDto>();
        }
    }

    private async Task InitializePhotoClusters(DateTime oldestDate, DateTime youngestDate, CancellationToken ct)
    {
        if (_clustersInitialized && _photoClusters != null)
            return;

        try
        {
                        var monthlyStats = await GetMonthlyPhotoStatistics(oldestDate, youngestDate, ct);
            
                        _photoClusters = CreateBalancedClusters(monthlyStats);
            _clustersInitialized = true;
        }
        catch (Exception)
        {
                        _photoClusters = new List<PhotoCluster>
            {
                new PhotoCluster
                {
                    StartDate = oldestDate,
                    EndDate = youngestDate,
                    PhotoCount = 1000,                     Weight = 1.0
                }
            };
            _clustersInitialized = true;
        }
    }

    private async Task<List<(DateTime Month, int PhotoCount)>> GetMonthlyPhotoStatistics(DateTime oldestDate, DateTime youngestDate, CancellationToken ct)
    {
        var monthlyStats = new List<(DateTime Month, int PhotoCount)>();
        var currentDate = new DateTime(oldestDate.Year, oldestDate.Month, 1);
        var endDate = new DateTime(youngestDate.Year, youngestDate.Month, 1);

        while (currentDate <= endDate)
        {
            var monthStart = currentDate;
            var monthEnd = currentDate.AddMonths(1).AddDays(-1);

                        if (monthStart < oldestDate) monthStart = oldestDate;
            if (monthEnd > youngestDate) monthEnd = youngestDate;

            try
            {
                var searchDto = new MetadataSearchDto
                {
                    Size = 1,                     Page = 1,
                    Type = AssetTypeEnum.IMAGE,
                    TakenAfter = monthStart,
                    TakenBefore = monthEnd.AddDays(1),                     WithExif = true,
                    Visibility = accountSettings.ShowArchived ? AssetVisibility.Archive : AssetVisibility.Timeline
                };

                var result = await immichApi.SearchAssetsAsync(searchDto, ct);
                var photoCount = result?.Assets?.Total ?? 0;
                
                monthlyStats.Add((currentDate, photoCount));
            }
            catch
            {
                                monthlyStats.Add((currentDate, 0));
            }

            currentDate = currentDate.AddMonths(1);
        }

        return monthlyStats;
    }

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

        foreach (var (month, photoCount) in monthlyStats)
        {
            if (firstMonth)
            {
                currentCluster.StartDate = month;
                firstMonth = false;
            }

            currentPhotoCount += photoCount;
            currentCluster.EndDate = month.AddMonths(1).AddDays(-1);

                                                var isLastMonth = month == monthlyStats.Last().Month;
            
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

        foreach (var cluster in clusters)
        {
            cluster.Weight = 1.0 / clusters.Count;
        }

        return clusters;
    }

    private PhotoCluster SelectRandomCluster()
    {
        if (_photoClusters == null || !_photoClusters.Any())
        {
            throw new InvalidOperationException("Foto-Cluster sind nicht initialisiert");
        }

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
}