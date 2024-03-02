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
                    _albumAssetInfos = GetAlbumAssetIds().ToDictionary(x=> Guid.Parse(x.Id));
                }

                return _albumAssetInfos;
            }
        }

        public AssetInfo? GetNextAsset()
        {
            return Settings.CurrentSettings.Albums.Any() ? GetRandomAlbumAsset() : GetRandomAsset();
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
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        var albumInfo = JsonDocument.Parse(responseContent);

                        var assets = albumInfo.RootElement.GetProperty("assets").ToString() ?? string.Empty;

                        var assetList = JsonSerializer.Deserialize<IEnumerable<AssetInfo>>(assets) ?? new List<AssetInfo>();

                        allAssets.AddRange(assetList);
                    }
                }
                return allAssets;
            }
        }

        private Random _random = new Random();
        private AssetInfo? GetRandomAlbumAsset()
        {
            var x = AlbumAssetInfos;

            var rnd = _random.Next(x.Count);

            return AlbumAssetInfos.ElementAt(rnd).Value;
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
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    var assetList = JsonSerializer.Deserialize<List<AssetInfo>>(responseContent);
                    if (assetList != null)
                    {
                        returnAsset = assetList.FirstOrDefault();
                    }
                }
            }
            return returnAsset;
        }
    }
}
