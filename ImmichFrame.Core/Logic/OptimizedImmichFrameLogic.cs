using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

public class OptimizedImmichFrameLogic : IImmichFrameLogic, IDisposable
{
    private readonly IServerSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ImmichApi _immichApi;
    private readonly ApiCache<IEnumerable<AssetResponseDto>> _apiCache;
    private readonly ILogger<OptimizedImmichFrameLogic> _logger;
    public OptimizedImmichFrameLogic(IServerSettings settings, ILogger<OptimizedImmichFrameLogic> logger)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.UseApiKey(_settings.ApiKey);
        _immichApi = new ImmichApi(_settings.ImmichServerUrl, _httpClient);
        _apiCache = new ApiCache<IEnumerable<AssetResponseDto>>(TimeSpan.FromMinutes(_settings.RefreshAlbumPeopleInterval));
    }

    public void Dispose()
    {
        _apiCache.Dispose();
        _httpClient.Dispose();
    }

    private Queue<AssetResponseDto> _assetQueue = new();
    private bool _isReloadingAssets = false;

    public Task<AssetResponseDto?> GetNextAsset()
    {
        if (_assetQueue.Count < 10 && !_isReloadingAssets)
        {
            // Fire-and-forget, reloading assets in the background
            _ = ReloadAssetsAsync();
        }

        if (_assetQueue.Any())
        {
            return Task.FromResult<AssetResponseDto?>(_assetQueue.Dequeue());
        }

        return Task.FromResult<AssetResponseDto?>(null);
    }

    private async Task ReloadAssetsAsync()
    {
        _isReloadingAssets = true;
        try
        {
            var assets = await GetAssets();
            foreach (var asset in assets)
            {
                _assetQueue.Enqueue(asset);
            }
        }
        finally
        {
            _isReloadingAssets = false;
        }
    }

    public Task<AssetResponseDto> GetAssetInfoById(Guid assetId)
    {
        return _immichApi.GetAssetInfoAsync(assetId, null);
    }

    private int _assetAmount = 250;
    private Random _random = new Random();
    public async Task<IEnumerable<AssetResponseDto>> GetAssets()
    {
        if (!_settings.ShowFavorites && !_settings.ShowMemories && !_settings.Albums.Any() && !_settings.People.Any())
        {
            return await GetRandomAssets();
        }

        IEnumerable<AssetResponseDto> assets = new List<AssetResponseDto>();

        if (_settings.ShowFavorites)
            assets = assets.Concat(await GetFavoriteAssets());
        if (_settings.ShowMemories)
            assets = assets.Concat(await GetMemoryAssets());
        if (_settings.Albums.Any())
            assets = assets.Concat(await GetAlbumAssets());
        if (_settings.People.Any())
            assets = assets.Concat(await GetPeopleAssets());

        // Display only Images
        assets = assets.Where(x => x.Type == AssetTypeEnum.IMAGE);

        if (!_settings.ShowArchived)
            assets = assets.Where(x => x.IsArchived == false);

        var takenBefore = _settings.ImagesUntilDate.HasValue ? _settings.ImagesUntilDate : null;
        if (takenBefore.HasValue)
        {
            assets = assets.Where(x => x.ExifInfo.DateTimeOriginal <= takenBefore);
        }

        var takenAfter = _settings.ImagesFromDate.HasValue ? _settings.ImagesFromDate : _settings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-_settings.ImagesFromDays.Value) : null;
        if (takenAfter.HasValue)
        {
            assets = assets.Where(x => x.ExifInfo.DateTimeOriginal >= takenAfter);
        }

        if (_settings.Rating is int rating)
        {
            assets = assets.Where(x => x.ExifInfo.Rating == rating);
        }

        if (_settings.ExcludedAlbums.Any())
        {
            var excludedAssetList = await GetExcludedAlbumAssets();
            var excludedAssetSet = excludedAssetList.Select(x => x.Id).ToHashSet();
            assets = assets.Where(x => !excludedAssetSet.Contains(x.Id));
        }

        assets = assets.OrderBy(asset => _random.Next());

        var assetsList = assets.ToList();
        if (assetsList.Count > _assetAmount)
        {
            assetsList = assetsList.Take(_assetAmount).ToList();
        }

        // Do not use for now
        // var updatedAssetsList = new List<AssetResponseDto>();
        // foreach (var asset in assetsList)
        // {
        //     var updatedAsset = await asset.LoadAdditionalAssetInfo(_immichApi, _logger);
        //     updatedAssetsList.Add(updatedAsset);
        // }
        // return updatedAssetsList;
        return assetsList;
    }

    public async Task<IEnumerable<AssetResponseDto>> GetRandomAssets()
    {
        var searchDto = new RandomSearchDto
        {
            Size = _assetAmount,
            Type = AssetTypeEnum.IMAGE,
            WithExif = true,
            WithPeople = true
        };

        if (_settings.ShowArchived)
        {
            searchDto.IsArchived = true;
        }

        var takenBefore = _settings.ImagesUntilDate.HasValue ? _settings.ImagesUntilDate : null;
        if (takenBefore.HasValue)
        {
            searchDto.TakenBefore = takenBefore;
        }

        var takenAfter = _settings.ImagesFromDate.HasValue ? _settings.ImagesFromDate : _settings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-_settings.ImagesFromDays.Value) : null;
        if (takenAfter.HasValue)
        {
            searchDto.TakenAfter = takenAfter;
        }

        if (_settings.Rating is int rating)
        {
            searchDto.Rating = rating;
        }

        var assets = await _immichApi.SearchRandomAsync(searchDto);

        if (_settings.ExcludedAlbums.Any())
        {
            var excludedAssetList = await GetExcludedAlbumAssets();
            var excludedAssetSet = excludedAssetList.Select(x => x.Id).ToHashSet();
            assets = assets.Where(x => !excludedAssetSet.Contains(x.Id)).ToList();
        }

        return assets;
    }

    public async Task<IEnumerable<AssetResponseDto>> GetMemoryAssets()
    {
        return await _apiCache.GetOrAddAsync("MemoryAssets", async () =>
        {
            var today = DateTime.Today;
            var memoryLane = await _immichApi.GetMemoryLaneAsync(today.Day, today.Month);

            var memoryAssets = new List<AssetResponseDto>();
            foreach (var lane in memoryLane)
            {
                var assets = lane.Assets.ToList();
                assets.ForEach(asset => asset.ImageDesc = $"{lane.YearsAgo} {(lane.YearsAgo == 1 ? "year" : "years")} ago");

                memoryAssets.AddRange(assets);
            }

            return memoryAssets;
        });
    }

    public async Task<IEnumerable<AssetResponseDto>> GetFavoriteAssets()
    {
        return await _apiCache.GetOrAddAsync("FavoriteAssets", async () =>
        {
            var favoriteAssets = new List<AssetResponseDto>();

            int page = 1;
            int batchSize = 1000;
            int total;
            do
            {
                var metadataBody = new MetadataSearchDto
                {
                    Page = page,
                    Size = batchSize,
                    IsFavorite = true,
                    Type = AssetTypeEnum.IMAGE,
                    WithExif = true,
                    WithPeople = true
                };

                var favoriteInfo = await _immichApi.SearchAssetsAsync(metadataBody);

                total = favoriteInfo.Assets.Total;

                favoriteAssets.AddRange(favoriteInfo.Assets.Items);
                page++;
            }
            while (total == batchSize);

            return favoriteAssets;
        });
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAlbumAssets()
    {
        return await _apiCache.GetOrAddAsync("AlbumAssets", async () =>
        {
            var albumAssets = new List<AssetResponseDto>();

            foreach (var albumId in _settings.Albums)
            {
                var albumInfo = await _immichApi.GetAlbumInfoAsync(albumId, null, null);

                albumAssets.AddRange(albumInfo.Assets);
            }

            return albumAssets;
        });
    }

    public async Task<IEnumerable<AssetResponseDto>> GetExcludedAlbumAssets()
    {
        return await _apiCache.GetOrAddAsync("ExcludedAlbumAssets", async () =>
        {
            var excludedAlbumAssets = new List<AssetResponseDto>();

            foreach (var albumId in _settings.ExcludedAlbums)
            {
                var albumInfo = await _immichApi.GetAlbumInfoAsync(albumId, null, null);

                excludedAlbumAssets.AddRange(albumInfo.Assets);
            }

            return excludedAlbumAssets;
        });
    }

    public async Task<IEnumerable<AssetResponseDto>> GetPeopleAssets()
    {
        return await _apiCache.GetOrAddAsync("PeopleAssets", async () =>
        {
            var personAssets = new List<AssetResponseDto>();

            foreach (var personId in _settings.People)
            {
                int page = 1;
                int batchSize = 1000;
                int total;
                do
                {
                    var metadataBody = new MetadataSearchDto
                    {
                        Page = page,
                        Size = batchSize,
                        PersonIds = new[] { personId },
                        Type = AssetTypeEnum.IMAGE,
                        WithExif = true,
                        WithPeople = true
                    };

                    var personInfo = await _immichApi.SearchAssetsAsync(metadataBody);

                    total = personInfo.Assets.Total;

                    personAssets.AddRange(personInfo.Assets.Items);
                    page++;
                }
                while (total == batchSize);
            }

            return personAssets;
        });
    }

    readonly string DownloadLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");
    public async Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id)
    {
        // Check if the image is already downloaded
        if (_settings.DownloadImages)
        {
            if (!Directory.Exists(DownloadLocation))
            {
                Directory.CreateDirectory(DownloadLocation);
            }

            var file = Directory.GetFiles(DownloadLocation).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == id.ToString());

            if (!string.IsNullOrWhiteSpace(file))
            {
                if (_settings.RenewImagesDuration > (DateTime.UtcNow - File.GetCreationTimeUtc(file)).Days)
                {
                    var fs = File.OpenRead(file);

                    var ex = Path.GetExtension(file);

                    return (Path.GetFileName(file), $"image/{ex}", fs);
                }

                File.Delete(file);
            }
        }

        var data = await _immichApi.ViewAssetAsync(id, string.Empty, AssetMediaSize.Preview);

        if (data == null)
            throw new AssetNotFoundException($"Asset {id} was not found!");

        var contentType = "";
        if (data.Headers.ContainsKey("Content-Type"))
        {
            contentType = data.Headers["Content-Type"].FirstOrDefault()?.ToString() ?? "";
        }
        var ext = contentType.ToLower() == "image/webp" ? "webp" : "jpeg";
        var fileName = $"{id}.{ext}";

        if (_settings.DownloadImages)
        {
            var stream = data.Stream;

            var filePath = Path.Combine(DownloadLocation, fileName);

            // save to folder
            var fs = File.Create(filePath);
            stream.CopyTo(fs);
            fs.Position = 0;
            return (Path.GetFileName(filePath), contentType, fs);
        }

        return (fileName, contentType, data.Stream);
    }

    public Task SendWebhookNotification(IWebhookNotification notification) => WebhookHelper.SendWebhookNotification(notification, _settings.Webhook);
}