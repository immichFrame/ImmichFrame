using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ThumbHashes;

namespace ImmichFrame.Helpers;

// Additional data partial
public static class AssetResponseDtoExtensionMethods
{
    [JsonIgnore]
    private static string? _imageDesc;

    public static void SetImageDesc(this AssetResponseDto dto, string desc) => _imageDesc = desc;
    public static string GetImageDesc(this AssetResponseDto dto)
    {
        if (string.IsNullOrWhiteSpace(_imageDesc))
            return dto.ExifInfo?.Description ?? string.Empty;

        return _imageDesc;
    }


    public static Stream? GetThumbHashStream(this AssetResponseDto dto)
    {
        if (dto.Thumbhash == null)
            return null;

        var hash = Convert.FromBase64String(dto.Thumbhash);
        var thumbhash = new ThumbHash(hash);
        return ImageHelper.SaveDataUrlToStream(thumbhash.ToDataUrl());
    }

    public static async Task<Stream> ServeImage(this AssetResponseDto dto, IImmichFrameLogic logic)
    {
        string localPath;
        if (PlatformDetector.IsDesktop())
        {
            localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Immich_Assets");
        }
        else
        {
            localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Immich_Assets");
        }

        if (!Directory.Exists(localPath))
        {
            Directory.CreateDirectory(localPath!);
        }

        var file = Directory.GetFiles(localPath!).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == dto.Id);

        if (!string.IsNullOrWhiteSpace(file))
        {
            if (Settings.CurrentSettings.RenewImagesDuration < (DateTime.UtcNow - File.GetCreationTimeUtc(file)).Days)
            {
                File.Delete(file);
            }
            else
            {
                return File.OpenRead(file);
            }
        }

        return await DownloadImageAsync(Guid.Parse(dto.Id), logic, localPath);
    }

    public static async Task<Stream> DownloadImageAsync(Guid id, IImmichFrameLogic logic, string localPath)
    {
        var data = await logic.GetImage(id);

        var contentType = "";
        if (data.Headers.ContainsKey("Content-Type"))
        {
            contentType = data.Headers["Content-Type"].ToString();
        }

        var stream = data.Stream;
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        if (!string.IsNullOrWhiteSpace(contentType) && Settings.CurrentSettings.DownloadImages)
        {
            var ext = contentType.ToLower() == "image/webp" ? "webp" : "jpeg";
            var filePath = Path.Combine(localPath, $"{id}.{ext}");

            // save to folder
            using (var fs = File.Create(filePath))
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

