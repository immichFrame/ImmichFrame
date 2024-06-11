using ImmichFrame.Helpers;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ThumbHashes;

namespace ImmichFrame.Models;

// Additional data partial
public partial class AssetResponseDto
{
    [JsonIgnore]
    private string? _imageDesc;

    [JsonIgnore]
    public string ImageDesc
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_imageDesc))
                return this.ExifInfo?.Description ?? string.Empty;

            return _imageDesc;
        }
        set
        {
            _imageDesc = value;
        }
    }

    [JsonIgnore]
    public Stream? ThumbhashImage => GetThumbHashStream();

    private Stream? GetThumbHashStream()
    {
        if (this.Thumbhash == null)
            return null;

        var hash = Convert.FromBase64String(this.Thumbhash);
        var thumbhash = new ThumbHash(hash);
        return ImageHelper.SaveDataUrlToStream(thumbhash.ToDataUrl());
    }

    [JsonIgnore]
    public Task<Stream> AssetImage => ServeImage();

    private async Task<Stream> ServeImage()
    {
        string localPath;
        if (PlatformDetector.IsDesktop())
        {
            localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Immich_Assets", $"{Id}.{Settings.CurrentSettings.PreviewFormat}");
        }
        else
        {
            localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Immich_Assets", $"{Id}.{Settings.CurrentSettings.PreviewFormat}");
        }
        var localDir = Path.GetDirectoryName(localPath);
        if (!Directory.Exists(localDir))
        {
            Directory.CreateDirectory(localDir!);
        }

        if (File.Exists(localPath))
        {
            if (Settings.CurrentSettings.RenewImagesDuration < (DateTime.UtcNow - File.GetCreationTimeUtc(localPath)).Days)
            {
                File.Delete(localPath);
            }
            else
            {
                return File.OpenRead(localPath);
            }
        }

        return await DownloadImageAsync(localPath);
    }
    private async Task<Stream> DownloadImageAsync(string localPath)
    {
        using (var client = new HttpClient())
        {
            var settings = Settings.CurrentSettings;

            client.UseApiKey(settings.ApiKey);

            var immichApi = new ImmichApi(settings.ImmichServerUrl, client);

            var data = await immichApi.ViewAssetAsync(Guid.Parse(this.Id), string.Empty,AssetMediaSize.Preview);

            var stream = data.Stream;
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            if (Settings.CurrentSettings.DownloadImages)
            {
                // save to folder
                using (var fs = File.Create(localPath))
                {
                    ms.CopyTo(fs);
                    ms.Position = 0;
                    return ms;
                }
            }
            else
            {
                return ms;
            }
        }
    }
}

