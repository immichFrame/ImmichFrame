using ImmichFrame.Exceptions;
using ImmichFrame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImmichFrame.Helpers
{
    public class AssetHelper
    {
        private Task<Dictionary<Guid, AssetResponseDto>?>? _filteredAssetInfos;
        private DateTime lastFilteredAssetRefesh;
        public Task<Dictionary<Guid, AssetResponseDto>?> FilteredAssetInfos
        {
            get
            {
                if (_filteredAssetInfos == null || lastFilteredAssetRefesh.DayOfYear != DateTime.Today.DayOfYear)
                {
                    lastFilteredAssetRefesh = DateTime.Now;
                    _filteredAssetInfos = GetFilteredAssetIds();
                }

                return _filteredAssetInfos;
            }
        }

        public async Task<AssetResponseDto?> GetNextAsset()
        {
            if ((await FilteredAssetInfos) != null)
            {
                return await GetRandomFilteredAsset();
            }

            return await GetRandomAsset();
        }

        private async Task<Dictionary<Guid, AssetResponseDto>?> GetFilteredAssetIds()
        {
            bool assetsAdded = false;
            IEnumerable<AssetResponseDto> list = new List<AssetResponseDto>();
            if (Settings.CurrentSettings.ShowMemories)
            {
                assetsAdded = true;
                list = list.Union(await GetMemoryAssets());
            }

            if (Settings.CurrentSettings.Albums?.Any() ?? false)
            {
                assetsAdded = true;
                list = list.Union(await GetAlbumAssets());
            }

            if (Settings.CurrentSettings.People?.Any() ?? false)
            {
                assetsAdded = true;
                list = list.Union(await GetPeopleAssets());
            }

            if (assetsAdded)
                // return only unique assets, no duplicates, only with Thumbnail
                return list.Where(x => x.Thumbhash != null).DistinctBy(x => x.Id).ToDictionary(x => Guid.Parse(x.Id));

            return null;
        }

        private async Task<IEnumerable<AssetResponseDto>> GetMemoryAssets()
        {
            using (var client = new HttpClient())
            {
                var settings = Settings.CurrentSettings;
                client.UseApiKey(settings.ApiKey);

                var immichApi = new ImmichApi(settings.ImmichServerUrl, client);

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
                    assets.ForEach(asset => asset.ImageDesc = lane.Title);

                    allAssets.AddRange(assets);
                }

                return allAssets;
            }
        }
        private async Task<IEnumerable<AssetResponseDto>> GetAlbumAssets()
        {
            using (var client = new HttpClient())
            {
                var allAssets = new List<AssetResponseDto>();
                var settings = Settings.CurrentSettings;

                var immichApi = new ImmichApi(settings.ImmichServerUrl, client);

                client.UseApiKey(settings.ApiKey);
                foreach (var albumId in settings.Albums!)
                {
                    try
                    {
                        var albumInfo = await immichApi.GetAlbumInfoAsync(albumId, null, null);

                        allAssets.AddRange(albumInfo.Assets);
                    }
                    catch (ApiException ex)
                    {
                        throw new AlbumNotFoundException($"Album '{albumId}' was not found, check your settings file!{Environment.NewLine}{Environment.NewLine}{ex.Message}", ex);
                    }
                }

                return allAssets;
            }
        }

        private async Task<IEnumerable<AssetResponseDto>> GetPeopleAssets()
        {
            using (var client = new HttpClient())
            {
                var allAssets = new List<AssetResponseDto>();
                var settings = Settings.CurrentSettings;

                var immichApi = new ImmichApi(settings.ImmichServerUrl, client);

                client.UseApiKey(settings.ApiKey);
                foreach (var personId in settings.People!)
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
            var settings = Settings.CurrentSettings;

            using (var client = new HttpClient())
            {
                client.UseApiKey(settings.ApiKey);

                var immichApi = new ImmichApi(settings.ImmichServerUrl, client);
                try
                {
                    var randomAssets = await immichApi.GetRandomAsync(null);

                    if (randomAssets.Any())
                    {
                        var asset = randomAssets.First();

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
