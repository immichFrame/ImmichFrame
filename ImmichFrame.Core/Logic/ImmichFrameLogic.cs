using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using System.Data;

namespace ImmichFrame.Core.Logic
{
    public class ImmichFrameLogic : IImmichFrameLogic
    {
        private IAccountSettings _accountSettings;
        private IGeneralSettings _frameSettings;
        public ImmichFrameLogic(IAccountSettings accountSettings, IGeneralSettings frameSettings)
        {
            _accountSettings = accountSettings;
            _frameSettings = frameSettings;
        }

        private Task<Dictionary<Guid, AssetResponseDto>?>? _filteredAssetInfos;
        private DateTime lastFilteredAssetRefesh;
        private List<Guid> ImmichFrameAlbumAssets = new List<Guid>();
        private static AlbumResponseDto immichFrameAlbum = new AlbumResponseDto();


        private Task<IEnumerable<Guid>>? _excludedAlbumAssets;
        private Task<IEnumerable<Guid>> ExcludedAlbumAssets
        {
            get
            {
                if (_excludedAlbumAssets == null)
                    _excludedAlbumAssets = GetExcludedAlbumAssets();

                return _excludedAlbumAssets;
            }
        }

        private Task<Dictionary<Guid, AssetResponseDto>?> FilteredAssetInfos
        {
            get
            {
                TimeSpan timeSinceRefresh = DateTime.Now - lastFilteredAssetRefesh;
                if (_filteredAssetInfos == null || timeSinceRefresh.TotalHours > _frameSettings.RefreshAlbumPeopleInterval)
                {
                    lastFilteredAssetRefesh = DateTime.Now;
                    _filteredAssetInfos = GetFilteredAssetIds();
                }

                return _filteredAssetInfos;
            }
        }

        public Task<AssetResponseDto> GetAssetInfoById(Guid assetId)
        {
            using (var client = new HttpClient())
            {
                client.UseApiKey(_accountSettings.ApiKey);
                var immichApi = new ImmichApi((_accountSettings).ImmichServerUrl, client);

                return immichApi.GetAssetInfoAsync(assetId, null);
            }
        }

        public async Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId)
        {
            using (var client = new HttpClient())
            {
                client.UseApiKey(_accountSettings.ApiKey);
                var immichApi = new ImmichApi(_accountSettings.ImmichServerUrl, client);

                return await immichApi.GetAllAlbumsAsync(assetId, null);
            }
        }

        private int _assetAmount = 250;
        public async Task<IEnumerable<AssetResponseDto>> GetAssets()
        {
            if ((await FilteredAssetInfos) != null)
            {
                return await GetRandomFilteredAssets();
            }

            return await GetRandomAssets();
        }

        public async Task<AssetResponseDto?> GetNextAsset()
        {
            if ((await FilteredAssetInfos) != null)
            {
                return await GetRandomFilteredAsset();
            }

            return await GetRandomAsset();
        }

        string DownloadLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");
        public async Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id)
        {
            if (_frameSettings.DownloadImages)
            {
                if (!Directory.Exists(DownloadLocation))
                {
                    Directory.CreateDirectory(DownloadLocation);
                }

                var file = Directory.GetFiles(DownloadLocation).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == id.ToString());

                if (!string.IsNullOrWhiteSpace(file))
                {
                    if (_frameSettings.RenewImagesDuration > (DateTime.UtcNow - File.GetCreationTimeUtc(file)).Days)
                    {
                        var fs = File.OpenRead(file);

                        var ext = Path.GetExtension(file);

                        return (Path.GetFileName(file), $"image/{ext}", fs);
                    }

                    File.Delete(file);
                }
            }

            using (var client = new HttpClient())
            {
                client.UseApiKey(_accountSettings.ApiKey);

                var immichApi = new ImmichApi(_accountSettings.ImmichServerUrl, client);

                var data = await immichApi.ViewAssetAsync(id, string.Empty, AssetMediaSize.Preview);

                if (data == null)
                    throw new AssetNotFoundException($"Asset {id} was not found!");

                var contentType = "";
                if (data.Headers.ContainsKey("Content-Type"))
                {
                    contentType = data.Headers["Content-Type"].FirstOrDefault()?.ToString() ?? "";
                }
                var ext = contentType.ToLower() == "image/webp" ? "webp" : "jpeg";
                var fileName = $"{id}.{ext}";

                if (_frameSettings.DownloadImages)
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
        }
        private async Task<Dictionary<Guid, AssetResponseDto>?> GetFilteredAssetIds()
        {
            bool assetsAdded = false;
            IEnumerable<AssetResponseDto> list = new List<AssetResponseDto>();
            if (_accountSettings.ShowMemories)
            {
                assetsAdded = true;
                list = list.Union(await GetMemoryAssets());
            }

            if (_accountSettings.ShowFavorites)
            {
                assetsAdded = true;
                list = list.Union(await GetRandomAssets());
            }

            if (_accountSettings.Albums?.Any() ?? false)
            {
                assetsAdded = true;
                list = list.Union(await GetAlbumAssets());
            }

            if (_accountSettings.People?.Any() ?? false)
            {
                assetsAdded = true;
                list = list.Union(await GetPeopleAssets());
            }

            if (_accountSettings.Rating.HasValue && list.Any())
            {
                list = list.Where(x => x.ExifInfo.Rating == _accountSettings.Rating.Value);
            }

            if (assetsAdded)
            {
                // Exclude videos
                list = list.Where(x => x.Type != AssetTypeEnum.VIDEO);

                var takenBefore = _accountSettings.ImagesUntilDate.HasValue ? _accountSettings.ImagesUntilDate : null;
                if (takenBefore.HasValue)
                {
                    list = list.Where(x => x.FileCreatedAt < takenBefore.Value);
                }

                var takenAfter = _accountSettings.ImagesFromDate.HasValue ? _accountSettings.ImagesFromDate : _accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-_accountSettings.ImagesFromDays.Value) : null;
                if (takenAfter.HasValue)
                {
                    list = list.Where(x => x.FileCreatedAt > takenAfter.Value);
                }

                var excludedList = await ExcludedAlbumAssets;

                // Exclude assets if configured
                if (excludedList.Any())
                    list = list.Where(x => !excludedList.Contains(Guid.Parse(x.Id)));

                // return only unique assets, no duplicates, only with Thumbnail
                return list.Where(x => x.Thumbhash != null).DistinctBy(x => x.Id).ToDictionary(x => Guid.Parse(x.Id));
            }

            return null;
        }
        private async Task<IEnumerable<AssetResponseDto>> GetMemoryAssets()
        {
            using (var client = new HttpClient())
            {
                client.UseApiKey(_accountSettings.ApiKey);

                var immichApi = new ImmichApi(_accountSettings.ImmichServerUrl, client);

                var allAssets = new List<AssetResponseDto>();

                var date = DateTime.Today;
                ICollection<MemoryResponseDto> memories;
                try
                {
                    memories = await immichApi.SearchMemoriesAsync(DateTime.Now, null, null, null);
                }
                catch (ApiException ex)
                {
                    throw new AlbumNotFoundException($"Memories were not found, check your settings file!{Environment.NewLine}{Environment.NewLine}{ex.Message}", ex);
                }

                foreach (var memory in memories)
                {
                    var assets = memory.Assets.ToList();
                    // var yearsAgo = DateTime.Now.Year - lane.Data.Year;
                    // assets.ForEach(asset => asset.ExifInfo.Description = $"{yearsAgo} {(yearsAgo == 1 ? "year" : "years")} ago");

                    allAssets.AddRange(assets);
                }

                return allAssets;
            }
        }
        private async Task<IEnumerable<AssetResponseDto>> GetAlbumAssets(Guid albumId, ImmichApi immichApi)
        {
            try
            {
                var albumInfo = await immichApi.GetAlbumInfoAsync(albumId, null, null);

                return _accountSettings.ShowArchived ? albumInfo.Assets : albumInfo.Assets.Where(x => !x.IsArchived);
            }
            catch (ApiException ex)
            {
                throw new AlbumNotFoundException($"Album '{albumId}' was not found, check your settings file!{Environment.NewLine}{Environment.NewLine}{ex.Message}", ex);
            }
        }
        private async Task<IEnumerable<AssetResponseDto>> GetAlbumAssets()
        {
            using var client = new HttpClient();

            var allAssets = new List<AssetResponseDto>();

            var immichApi = new ImmichApi(_accountSettings.ImmichServerUrl, client);

            client.UseApiKey(_accountSettings.ApiKey);
            foreach (var albumId in _accountSettings.Albums!)
            {
                allAssets.AddRange(await GetAlbumAssets(albumId, immichApi));
            }

            return allAssets;
        }
        private async Task<IEnumerable<Guid>> GetExcludedAlbumAssets()
        {
            using var client = new HttpClient();

            var allAssets = new List<AssetResponseDto>();

            var immichApi = new ImmichApi(_accountSettings.ImmichServerUrl, client);

            client.UseApiKey(_accountSettings.ApiKey);
            foreach (var albumId in _accountSettings.ExcludedAlbums!)
            {
                allAssets.AddRange(await GetAlbumAssets(albumId, immichApi));
            }

            return allAssets.Select(x => Guid.Parse(x.Id));
        }

        public Task<AssetStatsResponseDto> GetAssetStats()
        {
            using var client = new HttpClient();

            var allAssets = new List<AssetResponseDto>();

            var immichApi = new ImmichApi(_accountSettings.ImmichServerUrl, client);
            
            client.UseApiKey(_accountSettings.ApiKey);

            return immichApi.GetAssetStatisticsAsync(null, false, null);
        }
        
        private async Task<IEnumerable<AssetResponseDto>> GetPeopleAssets()
        {
            using (var client = new HttpClient())
            {
                var allAssets = new List<AssetResponseDto>();

                var immichApi = new ImmichApi(_accountSettings.ImmichServerUrl, client);

                client.UseApiKey(_accountSettings.ApiKey);
                foreach (var personId in _accountSettings.People!)
                {
                    try
                    {
                        int page = 1;
                        int batchSize = 1000;
                        int total = 0;
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

                            if (_accountSettings.ShowArchived)
                            {
                                metadataBody.Visibility = AssetVisibility.Archive;
                            }
                            else
                            {
                                metadataBody.Visibility = AssetVisibility.Timeline;
                            }

                            var takenBefore = _accountSettings.ImagesUntilDate.HasValue ? _accountSettings.ImagesUntilDate : null;
                            if (takenBefore.HasValue)
                            {
                                metadataBody.TakenBefore = takenBefore.Value;
                            }

                            var takenAfter = _accountSettings.ImagesFromDate.HasValue ? _accountSettings.ImagesFromDate : _accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-_accountSettings.ImagesFromDays.Value) : null;
                            if (takenAfter.HasValue)
                            {
                                metadataBody.TakenAfter = takenAfter.Value;
                            }

                            var personInfo = await immichApi.SearchAssetsAsync(metadataBody);

                            total = personInfo.Assets.Total;

                            allAssets.AddRange(personInfo.Assets.Items);
                            page++;
                        }
                        while (total == batchSize);
                    }
                    catch (ApiException ex)
                    {
                        throw new PersonNotFoundException($"Person '{personId}' was not found, check your settings file!{Environment.NewLine}{Environment.NewLine}{ex.Message}", ex);
                    }
                }

                // Remove duplicates
                var uniqueAssets = allAssets.DistinctBy(x => x.Id);

                return uniqueAssets;
            }
        }
        private Random _random = new Random();
        private async Task<AssetResponseDto?> GetRandomFilteredAsset()
        {
            var filteredAssetInfos = await FilteredAssetInfos;
            if (filteredAssetInfos == null || !filteredAssetInfos.Any())
                throw new AssetNotFoundException();

            var rnd = _random.Next(filteredAssetInfos.Count);

            return filteredAssetInfos.ElementAt(rnd).Value;
        }
        private async Task<List<AssetResponseDto>> GetRandomFilteredAssets()
        {
            var filteredAssetInfos = await FilteredAssetInfos;
            if (filteredAssetInfos == null || !filteredAssetInfos.Any())
                return new List<AssetResponseDto>();

            // If only memories, do not return random and order by date
            if (_accountSettings.ShowMemories && !_accountSettings.Albums.Any() && !_accountSettings.People.Any())
                return filteredAssetInfos.OrderBy(x => x.Value.ExifInfo.DateTimeOriginal).Select(x => x.Value).ToList();

            // Return randomly ordered list
            return filteredAssetInfos.OrderBy(asset => _random.Next(filteredAssetInfos.Count)).Take(_assetAmount).Select(x => x.Value).ToList();
        }

        List<AssetResponseDto> RandomAssetList = new List<AssetResponseDto>();
        private async Task<List<AssetResponseDto>> GetRandomAssets()
        {
            if (RandomAssetList.Any())
            {
                var assets = new List<AssetResponseDto>(RandomAssetList);
                RandomAssetList.Clear();

                return assets;
            }

            if (await LoadRandomAssets())
            {
                return await GetRandomAssets();
            }

            return new List<AssetResponseDto>();
        }

        private async Task<AssetResponseDto?> GetRandomAsset()
        {
            if (RandomAssetList.Any())
            {
                var randomAsset = RandomAssetList.First();
                RandomAssetList.Remove(randomAsset);

                // Skip this asset
                if (randomAsset.Thumbhash == null)
                    return await GetRandomAsset();

                return randomAsset;
            }

            if (await LoadRandomAssets())
            {
                return await GetRandomAsset();
            }

            return null;
        }

        private async Task<bool> LoadRandomAssets()
        {
            using (var client = new HttpClient())
            {
                client.UseApiKey(_accountSettings.ApiKey);

                var immichApi = new ImmichApi(_accountSettings.ImmichServerUrl, client);
                try
                {
                    var searchBody = new RandomSearchDto
                    {
                        Size = _assetAmount,
                        Type = AssetTypeEnum.IMAGE,
                        WithExif = true,
                        WithPeople = true,
                    };

                    if (_accountSettings.ShowArchived)
                    {
                        searchBody.Visibility = AssetVisibility.Archive;
                    }
                    else
                    {
                        searchBody.Visibility = AssetVisibility.Timeline;
                    }

                    if (_accountSettings.ShowFavorites)
                    {
                        searchBody.IsFavorite = true;
                    }

                    if (_accountSettings.Rating.HasValue)
                    {
                        searchBody.Rating = _accountSettings.Rating.Value;
                    }

                    var takenBefore = _accountSettings.ImagesUntilDate.HasValue ? _accountSettings.ImagesUntilDate : null;
                    if (takenBefore.HasValue)
                    {
                        searchBody.TakenBefore = takenBefore.Value;
                    }

                    var takenAfter = _accountSettings.ImagesFromDate.HasValue ? _accountSettings.ImagesFromDate : _accountSettings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-_accountSettings.ImagesFromDays.Value) : null;
                    if (takenAfter.HasValue)
                    {
                        searchBody.TakenAfter = takenAfter.Value;
                    }

                    var searchResponse = await immichApi.SearchRandomAsync(searchBody);

                    var randomAssets = searchResponse;

                    if (randomAssets.Any())
                    {
                        var excludedList = await ExcludedAlbumAssets;

                        randomAssets = randomAssets.Where(x => !excludedList.Contains(Guid.Parse(x.Id))).ToList();

                        RandomAssetList.AddRange(randomAssets);

                        return true;
                    }
                }
                catch (ApiException ex)
                {
                    throw new PersonNotFoundException($"Asset was not found, check your settings file!{Environment.NewLine}{Environment.NewLine}{ex.Message}", ex);
                }

                return false;
            }
        }

        public Task SendWebhookNotification(IWebhookNotification notification) => WebhookHelper.SendWebhookNotification(notification, _frameSettings.Webhook);
    }
}
