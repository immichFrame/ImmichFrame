using ImmichFrame.Helpers;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImmichFrame.Models;

// Additional data partial
public partial class AssetResponseDto
{
    [JsonIgnore]
    private string _imageDesc;

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
    public Task<Stream> AssetImage => ServeImage();

    private async Task<Stream> ServeImage()
    {
        var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Immich_Assets", $"{Id}.{ThumbnailFormat.JPEG}");
        var localDir = Path.GetDirectoryName(localPath);
        if (!Directory.Exists(localDir))
        {
            Directory.CreateDirectory(localDir);
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

            var data = await immichApi.GetAssetThumbnailAsync(ThumbnailFormat.JPEG, Guid.Parse(this.Id), null);

            var stream = data.Stream;
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            if (Settings.CurrentSettings.DownloadImages)
            {
                // save to folder
                using (var fs = File.Create(localPath))
                {
                    ms.Position = 0;
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

