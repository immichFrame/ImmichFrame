using ImmichFrame.Helpers;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImmichFrame.Models;

// Data from JSON partial
public partial class AssetInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("fileCreatedAt")]
    public DateTime FileCreatedAt { get; set; }
}

// Additional data partial
public partial class AssetInfo
{
    [JsonIgnore]
    public string ImageUrl => $"{Settings.CurrentSettings.ImmichServerUrl}/api/asset/thumbnail/{Id}?format={ImageExt}";

    [JsonIgnore]
    public string ImageExt => "JPEG";

    [JsonIgnore]
    public Task<Stream> AssetImage => ServeImage();

    private async Task<Stream> ServeImage()
    {
        var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Immich_Assets", $"{Id}.{ImageExt}");
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
            client.UseApiKey(Settings.CurrentSettings.ApiKey);
            var data = await client.GetByteArrayAsync(this.ImageUrl);

            var stream = new MemoryStream(data);
            if (Settings.CurrentSettings.DownloadImages)
            {
                // save to folder
                using (var fs = File.Create(localPath))
                {
                    stream.CopyTo(fs);
                    stream.Position = 0;
                    return stream;
                }
            }
            else
            {
                return stream;
            }
        }
    }
}

