using ImmichFrame.Exceptions;
using ImmichFrame.Models;
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
        private Task<Dictionary<Guid, AssetResponseDto>> _memoryAssetInfos;
        private DateTime lastMemoryAssetRefesh;
        public Task<Dictionary<Guid, AssetResponseDto>> MemoryAssetInfos
        {
            get
            {
                if (_memoryAssetInfos == null || lastMemoryAssetRefesh.DayOfYear != DateTime.Today.DayOfYear)
                {
                    lastMemoryAssetRefesh = DateTime.Now;
                    _memoryAssetInfos = GetMemoryAssetIds();
                }

                return _memoryAssetInfos;
            }
        }

        private Task<Dictionary<Guid, AssetResponseDto>> _albumAssetInfos;
        private DateTime lastAlbumAssetRefesh;
        public Task<Dictionary<Guid, AssetResponseDto>> AlbumAssetInfos
        {
            get
            {
                // Refresh if no assets loaded or lastAlbumRefesh is older than one day
                // TODO: Put refresh duration in config
                if (_albumAssetInfos == null || lastAlbumAssetRefesh.AddDays(1) < DateTime.Now)
                {
                    lastAlbumAssetRefesh = DateTime.Now;
                    _albumAssetInfos = GetAlbumAssetIds();
                }

                return _albumAssetInfos;
            }
        }
        private Task<Dictionary<Guid, AssetResponseDto>> _peopleAssetInfos;
        private DateTime lastPeopleAssetRefesh;
        public Task<Dictionary<Guid, AssetResponseDto>> PeopleAssetInfos
        {
            get
            {
                // Refresh if no assets loaded or lastAlbumRefesh is older than one day
                // TODO: Put refresh duration in config
                if (_peopleAssetInfos == null || lastPeopleAssetRefesh.AddDays(1) < DateTime.Now)
                {
                    lastPeopleAssetRefesh = DateTime.Now;
                    _peopleAssetInfos = GetPeopleAssetIds();
                }

                return _peopleAssetInfos;
            }
        }

        public async Task<AssetResponseDto?> GetNextAsset()
        {
            if (Settings.CurrentSettings.OnlyMemories)
            {
                return await GetRandomMemoryAsset();
            }

            //return Settings.CurrentSettings.Albums.Any() ? await GetRandomAlbumAsset() : await GetRandomAsset();
            return Settings.CurrentSettings.Albums.Any() ? await GetRandomAlbumAsset() : Settings.CurrentSettings.People.Any() ? await GetRandomPeopleAsset() : await GetRandomAsset();
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

                var x = new ImmichApi(settings.ImmichServerUrl, client);

                client.UseApiKey(settings.ApiKey);
                foreach (var albumId in settings.Albums!)
                {
                    try
                    {
                        var albumInfo = await x.GetAlbumInfoAsync(albumId, null, null);

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

                var x = new ImmichApi(settings.ImmichServerUrl, client);

                client.UseApiKey(settings.ApiKey);
                foreach (var personId in settings.People!)
                {
                    try
                    {
                        var personInfo = await x.GetPersonAssetsAsync(personId);

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
        private async Task<AssetResponseDto?> GetRandomAlbumAsset()
        {
            var albumAssetInfos = await AlbumAssetInfos;
            if (!albumAssetInfos.Any())
                throw new AssetNotFoundException();

            var rnd = _random.Next(albumAssetInfos.Count);

            return albumAssetInfos.ElementAt(rnd).Value;
        }
        private async Task<AssetResponseDto?> GetRandomPeopleAsset()
        {
            var peopleAssetInfos = await PeopleAssetInfos;
            if (!peopleAssetInfos.Any())
                throw new AssetNotFoundException();

            var rnd = _random.Next(peopleAssetInfos.Count);

            return peopleAssetInfos.ElementAt(rnd).Value;
        }

        private async Task<AssetResponseDto?> GetRandomMemoryAsset()
        {
            var memoryAssetInfos = await MemoryAssetInfos;
            if (!memoryAssetInfos.Any())
                throw new AssetNotFoundException();

            var rnd = _random.Next(memoryAssetInfos.Count);

            return memoryAssetInfos.ElementAt(rnd).Value;
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
