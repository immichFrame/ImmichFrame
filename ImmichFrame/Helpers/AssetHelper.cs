using ImmichFrame.Exceptions;
using ImmichFrame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace ImmichFrame.Helpers
{
    public class AssetHelper
    {
        private Dictionary<Guid, AssetInfo> _memoryAssetInfos;
        private DateTime lastMemoryAssetRefesh;
        public Dictionary<Guid, AssetInfo> MemoryAssetInfos
        {
            get
            {
                if (_memoryAssetInfos == null || lastMemoryAssetRefesh.DayOfYear != DateTime.Today.DayOfYear)
                {
                    lastMemoryAssetRefesh = DateTime.Now;
                    _memoryAssetInfos = GetMemoryAssetIds().ToDictionary(x => Guid.Parse(x.Id));
                }

                return _memoryAssetInfos;
            }
        }

        private Dictionary<Guid, AssetInfo> _albumAssetInfos;
        private DateTime lastAlbumAssetRefesh;
        public Dictionary<Guid, AssetInfo> AlbumAssetInfos
        {
            get
            {
                // Refresh if no assets loaded or lastAlbumRefesh is older than one day
                // TODO: Put refresh duration in config
                if (_albumAssetInfos == null || lastAlbumAssetRefesh.AddDays(1) < DateTime.Now)
                {
                    lastAlbumAssetRefesh = DateTime.Now;
                    _albumAssetInfos = GetAlbumAssetIds().ToDictionary(x => Guid.Parse(x.Id));
                }

                return _albumAssetInfos;
            }
        }

        public AssetInfo? GetNextAsset()
        {
            if (Settings.CurrentSettings.OnlyMemories)
            {
                return GetRandomMemoryAsset();
            }

            return Settings.CurrentSettings.Albums.Any() ? GetRandomAlbumAsset() : GetRandomAsset();
        }

        private IEnumerable<AssetInfo> GetMemoryAssetIds()
        {
            using (var client = new HttpClient())
            {
                var settings = Settings.CurrentSettings;
                var allAssets = new List<AssetInfo>();

                client.UseApiKey(settings.ApiKey);

                var date = DateTime.Today;

                string url = $"{settings.ImmichServerUrl}/api/asset/memory-lane?day={date.Day}&month={date.Month}";

                var response = client.GetAsync(url).Result;

                if (!response.IsSuccessStatusCode)
                    throw new AlbumNotFoundException($"Memories could not be loaded, check your settings file");

                var responseContent = response.Content.ReadAsStringAsync().Result;

                var albumInfo = JsonDocument.Parse(responseContent);

                var years = albumInfo.RootElement.EnumerateArray().Cast<JsonElement>().ToList();

                foreach (var year in years)
                {
                    var assets = year.GetProperty("assets").ToString() ?? string.Empty;
                    var assetList = JsonSerializer.Deserialize<IEnumerable<AssetInfo>>(assets) ?? new List<AssetInfo>();

                    var title = year.GetProperty("title").ToString() ?? string.Empty;

                    assetList.ToList().ForEach(asset => asset.ImageDesc = title);

                    allAssets.AddRange(assetList);
                }

                return allAssets;
            }
        }

        private IEnumerable<AssetInfo> GetAlbumAssetIds()
        {
            using (var client = new HttpClient())
            {
                var allAssets = new List<AssetInfo>();
                var settings = Settings.CurrentSettings;

                client.UseApiKey(settings.ApiKey);
                foreach (var albumId in settings.Albums!)
                {
                    string url = $"{settings.ImmichServerUrl}/api/album/{albumId}";

                    var response = client.GetAsync(url).Result;

                    if (!response.IsSuccessStatusCode)
                        throw new AlbumNotFoundException($"Album '{albumId}' was not found, check your settings file");

                    var responseContent = response.Content.ReadAsStringAsync().Result;

                    var albumInfo = JsonDocument.Parse(responseContent);

                    var assets = albumInfo.RootElement.GetProperty("assets").ToString() ?? string.Empty;

                    var assetList = JsonSerializer.Deserialize<IEnumerable<AssetInfo>>(assets) ?? new List<AssetInfo>();

                    allAssets.AddRange(assetList);
                }

                return allAssets;
            }
        }

        private Random _random = new Random();
        private AssetInfo? GetRandomAlbumAsset()
        {
            if (!AlbumAssetInfos.Any())
                throw new AssetNotFoundException();

            var rnd = _random.Next(AlbumAssetInfos.Count);

            return AlbumAssetInfos.ElementAt(rnd).Value;
        }
        private AssetInfo? GetRandomMemoryAsset()
        {
            if (!MemoryAssetInfos.Any())
                throw new AssetNotFoundException();

            var rnd = _random.Next(MemoryAssetInfos.Count);

            return MemoryAssetInfos.ElementAt(rnd).Value;
        }
        private AssetInfo? GetRandomAsset()
        {
            AssetInfo? returnAsset = null;
            var settings = Settings.CurrentSettings;

            string url = $"{settings.ImmichServerUrl}/api/asset/random";
            using (var client = new HttpClient())
            {
                client.UseApiKey(settings.ApiKey);
                var response = client.GetAsync(url).Result;

                if (!response.IsSuccessStatusCode)
                    throw new AssetNotFoundException();

                var responseContent = response.Content.ReadAsStringAsync().Result;
                var assetList = JsonSerializer.Deserialize<List<AssetInfo>>(responseContent);
                if (assetList != null)
                {
                    returnAsset = assetList.FirstOrDefault();
                }
            }

            return returnAsset;
        }
    }
}
