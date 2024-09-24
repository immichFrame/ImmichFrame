using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Exceptions;
using System.Data;

namespace ImmichFrame.Core.Logic
{
    public class ImmichFrameLogic : IImmichFrameLogic
    {
        private IBaseSettings _settings;
        public ImmichFrameLogic(IBaseSettings settings)
        {
            _settings = settings;
        }

        private Task<Dictionary<Guid, AssetResponseDto>?>? _filteredAssetInfos;
        private DateTime lastFilteredAssetRefesh;
        private List<Guid> ImmichFrameAlbumAssets = new List<Guid>();
        private static AlbumResponseDto immichFrameAlbum = new AlbumResponseDto();
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

        public async Task<AssetResponseDto> GetNextAsset()
        {
            if ((await FilteredAssetInfos) != null)
            {
                return await GetRandomFilteredAsset();
            }

            return await GetRandomAsset();
        }

        public async Task<FileResponse> GetImage(Guid id)
        {
            using (var client = new HttpClient())
            {
                client.UseApiKey(_settings.ApiKey);

                var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);

                return await immichApi.ViewAssetAsync(id, string.Empty, AssetMediaSize.Preview);
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

            if (assetsAdded)
            {
                // Exclude videos
                list = list.Where(x => x.Type != AssetTypeEnum.VIDEO);

                var excludedalbumAssetIds = (await GetExcludedAlbumAssets()).Select(x => x.Id);

                // Exclude assets if configured
                if (excludedalbumAssetIds.Any())
                    list = list.Where(x => !excludedalbumAssetIds.Contains(x.Id));

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

                return albumInfo.Assets;
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
        private async Task<IEnumerable<AssetResponseDto>> GetExcludedAlbumAssets()
        {
            using var client = new HttpClient();

            var allAssets = new List<AssetResponseDto>();

            var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);

            client.UseApiKey(_settings.ApiKey);
            foreach (var albumId in _settings.ExcludedAlbums!)
            {
                allAssets.AddRange(await GetAlbumAssets(albumId, immichApi));
            }

            return allAssets;
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
                        var personInfo = await immichApi.GetPersonAssetsAsync(personId);

                        allAssets.AddRange(personInfo);
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
        private async Task<AssetResponseDto?> GetRandomAsset()
        {
            using (var client = new HttpClient())
            {
                client.UseApiKey(_settings.ApiKey);

                var immichApi = new ImmichApi(_settings.ImmichServerUrl, client);
                try
                {
                    var searchBody = new RandomSearchDto
                    {
                        Size = 1,
                        Page = 1
                    };
                    var searchResponse = await immichApi.SearchRandomAsync(searchBody);

                    var randomAssets = searchResponse.Assets.Items;

                    if (randomAssets.Any())
                    {
                        var asset = randomAssets.First();

                        if (asset.Type == AssetTypeEnum.VIDEO)
                            return await GetRandomAsset();

                        var albumIds = (await immichApi.GetAllAlbumsAsync(Guid.Parse(asset.Id), true)).Select(x => Guid.Parse(x.Id));

                        // Reload if exclude album is configured
                        if (_settings.ExcludedAlbums.Any(x => albumIds.Contains(x)))
                        {
                            return await GetRandomAsset();
                        }

                        // do not return with no thumbnail
                        if (asset.Thumbhash == null)
                            return await GetRandomAsset();

                        return randomAssets.First();
                    }
                }
                catch (ApiException ex)
                {
                    throw new PersonNotFoundException($"Asset was not found, check your settings file!{Environment.NewLine}{Environment.NewLine}{ex.Message}", ex);
                }
            }

            return null;
        }
    }
}
