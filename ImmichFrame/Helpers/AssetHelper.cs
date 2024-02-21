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
        private Settings _settings;

        private List<AssetInfo> _albumAssetInfos;
        private DateTime lastAlbumAssetRefesh;
        public List<AssetInfo> AlbumAssetInfos
        {
            get
            {
                // Refresh if no assets loaded or lastAlbumRefesh is older than one day
                // TODO: Put refresh duration in config
                if (_albumAssetInfos == null || lastAlbumAssetRefesh.AddDays(1) < DateTime.Now)
                {
                    lastAlbumAssetRefesh = DateTime.Now;
                    _albumAssetInfos = GetAlbumAssetIds().ToList();
                }

                return _albumAssetInfos;
            }
        }

        public AssetHelper(Settings settings)
        {
            _settings = settings;
        }

        public AssetInfo? GetNextAsset()
        {
            return _settings.Albums!.Any() ? GetRandomAlbumAsset() : GetRandomAsset();
        }

        private IEnumerable<AssetInfo> GetAlbumAssetIds()
        {
            using (var client = new HttpClient())
            {
                var bigList = new List<AssetInfo>();

                client.UseApiKey(_settings.ApiKey);
                foreach (var albumId in _settings.Albums!)
                {
                    string url = $"{_settings.ImmichServerUrl}/api/album/{albumId}";

                    var response = client.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        var albumInfo = JsonDocument.Parse(responseContent);

                        var assets = albumInfo.RootElement.GetProperty("assets").ToString() ?? string.Empty;

                        var assetList = JsonSerializer.Deserialize<IEnumerable<AssetInfo>>(assets) ?? new List<AssetInfo>();

                        bigList.AddRange(assetList);
                    }
                }
                return bigList;
            }
        }

        private Random _random = new Random();
        private AssetInfo? GetRandomAlbumAsset()
        {
            var x = AlbumAssetInfos;

            var rnd = _random.Next(x.Count());

            return AlbumAssetInfos[rnd];
        }
        private AssetInfo? GetRandomAsset()
        {
            AssetInfo? returnAsset = null;

            string url = $"{_settings.ImmichServerUrl}/api/asset/random";
            using (var client = new HttpClient())
            {
                client.UseApiKey(_settings.ApiKey);
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
