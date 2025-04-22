using Ical.Net;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;
using OpenWeatherMap;
using System.Data;
using System.Text.Json;

namespace ImmichFrame.Core.Logic
{
    public class ImmichFrameLogic : IImmichFrameLogic
    {
        private IServerSettings _settings;
        public ImmichFrameLogic(IServerSettings settings)
        {
            _settings = settings;
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
                if (_filteredAssetInfos == null || timeSinceRefresh.TotalHours > _settings.RefreshAlbumPeopleInterval)
                {
                    lastFilteredAssetRefesh = DateTime.Now;
                    _filteredAssetInfos = GetFilteredAssetIds();
                }

                return _filteredAssetInfos;
            }
        }

        private int _assetAmount = 250;
        public async Task<List<AssetResponseDto>> GetAssets()
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

                        var ext = Path.GetExtension(file);

                        return (Path.GetFileName(file), $"image/{ext}", fs);
                    }

                    File.Delete(file);
                }
            }

            using (var client = new HttpClient())
            {
                client.UseApiKey(_settings.ApiKey);

                var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);

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
        }

        public async Task AddAssetToAlbum(AssetResponseDto assetToAdd)
        {
            using (var client = new HttpClient())
            {
                client.UseApiKey(_settings.ApiKey);
                var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);
                var itemsToAdd = new BulkIdsDto();
                itemsToAdd.Ids.Add(new Guid(assetToAdd.Id));
                await immichApi.AddAssetsToAlbumAsync(new Guid(immichFrameAlbum.Id), null, itemsToAdd);
                ImmichFrameAlbumAssets.Add(new Guid(assetToAdd.Id));
                //only keep 100 most recent assets in album
                var albumInfo = await immichApi.GetAlbumInfoAsync(new Guid(immichFrameAlbum.Id), null, null);
                if (albumInfo.AssetCount > 100)
                {
                    var itemToRemove = new BulkIdsDto();
                    itemToRemove.Ids.Add(ImmichFrameAlbumAssets[0]);
                    await immichApi.RemoveAssetFromAlbumAsync(new Guid(immichFrameAlbum.Id), itemToRemove);
                    ImmichFrameAlbumAssets.RemoveAt(0);
                }
            }
        }
        public async Task DeleteAndCreateImmichFrameAlbum()
        {
            using (var client = new HttpClient())
            {
                client.UseApiKey(_settings.ApiKey);
                var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);
                var immichAlbums = await immichApi.GetAllAlbumsAsync(null, null);
                immichFrameAlbum = immichAlbums.FirstOrDefault(album => album.AlbumName == _settings.ImmichFrameAlbumName)!;
                if (immichFrameAlbum != null)
                {
                    await immichApi.DeleteAlbumAsync(new Guid(immichFrameAlbum.Id));
                }
                var albumDto = new CreateAlbumDto
                {
                    AlbumName = _settings.ImmichFrameAlbumName,
                    Description = "Recent ImmichFrame Photos"
                };
                var result = await immichApi.CreateAlbumAsync(albumDto);
                immichFrameAlbum = new AlbumResponseDto { Id = result.Id };
            }
        }

        private async Task<Dictionary<Guid, AssetResponseDto>?> GetFilteredAssetIds()
        {
            bool assetsAdded = false;
            IEnumerable<AssetResponseDto> list = new List<AssetResponseDto>();
            if (_settings.ShowMemories)
            {
                assetsAdded = true;
                list = list.Union(await GetMemoryAssets());
            }

            if (_settings.ShowFavorites)
            {
                assetsAdded = true;
                list = list.Union(await GetRandomAssets());
            }

            if (_settings.Albums?.Any() ?? false)
            {
                assetsAdded = true;
                list = list.Union(await GetAlbumAssets());
            }

            if (_settings.People?.Any() ?? false)
            {
                assetsAdded = true;
                list = list.Union(await GetPeopleAssets());
            }

            if (_settings.Rating.HasValue && list.Any())
            {
                list = list.Where(x => x.ExifInfo.Rating == _settings.Rating.Value);
            }

            if (assetsAdded)
            {
                // Exclude videos
                list = list.Where(x => x.Type != AssetTypeEnum.VIDEO);

                var takenBefore = _settings.ImagesUntilDate.HasValue ? _settings.ImagesUntilDate : null;
                if (takenBefore.HasValue)
                {
                    list = list.Where(x => x.FileCreatedAt < takenBefore.Value);
                }

                var takenAfter = _settings.ImagesFromDate.HasValue ? _settings.ImagesFromDate : _settings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-_settings.ImagesFromDays.Value) : null;
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
                client.UseApiKey(_settings.ApiKey);

                var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);

                var allAssets = new List<AssetResponseDto>();

                var date = DateTime.Today;
                ICollection<MemoryLaneResponseDto> memoryLane;
                try
                {
                    memoryLane = await immichApi.GetMemoryLaneAsync(date.Day, date.Month);
                }
                catch (ApiException ex)
                {
                    throw new AlbumNotFoundException($"Memories were not found, check your settings file!{Environment.NewLine}{Environment.NewLine}{ex.Message}", ex);
                }

                foreach (var lane in memoryLane)
                {
                    var assets = lane.Assets.ToList();
                    assets.ForEach(asset => asset.ImageDesc = $"{lane.YearsAgo} {(lane.YearsAgo == 1 ? "year" : "years")} ago");

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

                return _settings.ShowArchived ? albumInfo.Assets : albumInfo.Assets.Where(x => !x.IsArchived);
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

            var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);

            client.UseApiKey(_settings.ApiKey);
            foreach (var albumId in _settings.Albums!)
            {
                allAssets.AddRange(await GetAlbumAssets(albumId, immichApi));
            }

            return allAssets;
        }
        private async Task<IEnumerable<Guid>> GetExcludedAlbumAssets()
        {
            using var client = new HttpClient();

            var allAssets = new List<AssetResponseDto>();

            var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);

            client.UseApiKey(_settings.ApiKey);
            foreach (var albumId in _settings.ExcludedAlbums!)
            {
                allAssets.AddRange(await GetAlbumAssets(albumId, immichApi));
            }

            return allAssets.Select(x => Guid.Parse(x.Id));
        }
        private async Task<IEnumerable<AssetResponseDto>> GetPeopleAssets()
        {
            using (var client = new HttpClient())
            {
                var allAssets = new List<AssetResponseDto>();

                var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);

                client.UseApiKey(_settings.ApiKey);
                foreach (var personId in _settings.People!)
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
                            if (!_settings.ShowArchived)
                            {
                                metadataBody.IsArchived = false;
                            }
                            var takenBefore = _settings.ImagesUntilDate.HasValue ? _settings.ImagesUntilDate : null;
                            if (takenBefore.HasValue)
                            {
                                metadataBody.TakenBefore = takenBefore.Value;
                            }

                            var takenAfter = _settings.ImagesFromDate.HasValue ? _settings.ImagesFromDate : _settings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-_settings.ImagesFromDays.Value) : null;
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
            if (_settings.ShowMemories && !_settings.Albums.Any() && !_settings.People.Any())
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
                client.UseApiKey(_settings.ApiKey);

                var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);
                try
                {
                    var searchBody = new RandomSearchDto
                    {
                        Size = _assetAmount,
                        Type = AssetTypeEnum.IMAGE,
                        WithExif = true,
                        WithPeople = true,
                    };
                    
                    if (!_settings.ShowArchived)
                    {
                        searchBody.IsArchived = false;
                    }

                    if (_settings.ShowFavorites)
                    {
                        searchBody.IsFavorite = true;
                    }

                    if (_settings.Rating.HasValue)
                    {
                        searchBody.Rating = _settings.Rating.Value;
                    }

                    var takenBefore = _settings.ImagesUntilDate.HasValue ? _settings.ImagesUntilDate : null;
                    if (takenBefore.HasValue)
                    {
                        searchBody.TakenBefore = takenBefore.Value;
                    }

                    var takenAfter = _settings.ImagesFromDate.HasValue ? _settings.ImagesFromDate : _settings.ImagesFromDays.HasValue ? DateTime.Today.AddDays(-_settings.ImagesFromDays.Value) : null;
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

        private (DateTime fetchDate, IWeather? weather)? lastWeather;

        public async Task<IWeather?> GetWeather()
        {
            // Check if cached weather data is still valid
            if (lastWeather != null && lastWeather.Value.fetchDate.AddMinutes(5) > DateTime.Now)
            {
                return lastWeather.Value.weather;
            }

            OpenWeatherMapOptions options = new OpenWeatherMapOptions
            {
                ApiKey = _settings.WeatherApiKey,
                UnitSystem = _settings.UnitSystem,
                Language = _settings.Language,
            };

            var weatherLatLong = _settings.WeatherLatLong;

            var weatherLat = !string.IsNullOrWhiteSpace(weatherLatLong) ? float.Parse(weatherLatLong!.Split(',')[0]) : 0f;
            var weatherLong = !string.IsNullOrWhiteSpace(weatherLatLong) ? float.Parse(weatherLatLong!.Split(',')[1]) : 0f;

            var weather = await GetWeather(weatherLat, weatherLong, options);

            lastWeather = (DateTime.Now, weather);

            return weather;
        }

        public async Task<IWeather?> GetWeather(double latitude, double longitude, OpenWeatherMapOptions Options)
        {
            try
            {
                IOpenWeatherMapService openWeatherMapService = new OpenWeatherMapService(Options);
                var weatherInfo = await openWeatherMapService.GetCurrentWeatherAsync(latitude, longitude);

                return weatherInfo.ToWeather();
            }
            catch
            {
                //do nothing and return null
            }

            return null;
        }

        private (DateTime fetchDate, List<string> calendars)? lastCalendars;
        public async Task<List<string>> GetCalendars()
        {
            if (lastCalendars != null && lastCalendars.Value.fetchDate.AddMinutes(15) > DateTime.Now)
            {
                return lastCalendars.Value.calendars;
            }

            var icals = new List<string>();

            foreach (var webcal in _settings.Webcalendars)
            {
                string httpUrl = webcal.Replace("webcal://", "https://");

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(httpUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        icals.Add(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        throw new Exception("Failed to load calendar data");
                    }
                }
            }

            lastCalendars = (DateTime.Now, icals);

            return icals;
        }

        public async Task<List<IAppointment>> GetAppointments()
        {
            var appointments = new List<IAppointment>();

            var icals = await GetCalendars();

            foreach (var ical in icals)
            {
                var calendar = Calendar.Load(ical);

                appointments.AddRange(calendar.GetOccurrences(DateTime.UtcNow, DateTime.Today.AddDays(1)).Select(x => x.ToAppointment()));
            }

            return appointments;
        }

        public async Task SendWebhookNotification(IWebhookNotification notification)
        {
            if (string.IsNullOrWhiteSpace(_settings.Webhook)) return;

            var httpClient = new HttpClient();

            var options = new JsonSerializerOptions
            {
                Converters = { new PolymorphicJsonConverter<IWebhookNotification>() },
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(notification, options);
            var data = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_settings.Webhook, data);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Webhook successfully sent.");
            }
            else
            {
                Console.WriteLine($"Failed to send notification to webhook: {Convert.ToInt32(response.StatusCode)} {response.StatusCode}");
            }
        }
    }
}
