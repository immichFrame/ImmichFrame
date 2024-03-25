using ImmichFrame.Exceptions;
using ImmichFrame.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImmichFrame.Helpers
{
    public class AssetHelper
    {
        private Task<Dictionary<Guid, AssetResponseDto>?> _filteredAssetInfos;
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
            var list = new Dictionary<Guid, AssetResponseDto>();
            if (Settings.CurrentSettings.ShowMemories)
            {
                assetsAdded = true;
                list = list.Union(await GetMemoryAssetIds()).ToDictionary(x => x.Key, x => x.Value);
            }

            if (Settings.CurrentSettings.Albums.Any())
            {
                assetsAdded = true;
                list = list.Union(await GetAlbumAssetIds()).ToDictionary(x=>x.Key, x=>x.Value);
            }

            if (Settings.CurrentSettings.People.Any())
            {
                assetsAdded = true;
                list = list.Union(await GetPeopleAssetIds()).ToDictionary(x => x.Key, x => x.Value);
            }

            if (assetsAdded)
                return list;

            return null;
        }

        private async Task<Dictionary<Guid, AssetResponseDto>> GetMemoryAssetIds()
        {
            using (var client = new HttpClient())
            {
                var settings = Settings.CurrentSettings;
                client.UseApiKey(settings.ApiKey);

                var immichApi = new ImmichApi(settings.ImmichServerUrl, client);

                var allAssets = new List<AssetResponseDto>();

                var date = DateTime.Today;

                var memoryLane = await immichApi.GetMemoryLaneAsync(date.Day, date.Month);

                foreach (var lane in memoryLane)
                {
                    var assets = lane.Assets.ToList();
                    assets.ForEach(asset => asset.ImageDesc = lane.Title);

                    allAssets.AddRange(assets);
                }

                return allAssets.ToDictionary(x => Guid.Parse(x.Id));
            }
        }
        private async Task<Dictionary<Guid, AssetResponseDto>> GetAlbumAssetIds()
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
                        throw new AlbumNotFoundException($"Album '{albumId}' was not found, check your settings file", ex);
                    }
                }

                return allAssets.ToDictionary(x => Guid.Parse(x.Id));
            }
        }
        private async Task<Dictionary<Guid, AssetResponseDto>> GetPeopleAssetIds()
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
                        throw new PersonNotFoundException($"Person '{personId}' was not found, check your settings file", ex);
                    }
                }
                // Remove duplicates
                var uniqueAssets = allAssets.GroupBy(x => x.Id).Select(group => group.First());
                return uniqueAssets.ToDictionary(x => Guid.Parse(x.Id));
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

                var randomAssets = await immichApi.GetRandomAsync(null);

                if (randomAssets.Any())
                {
                    return randomAssets.First();
                }
            }

            return null;
        }
    }
}
