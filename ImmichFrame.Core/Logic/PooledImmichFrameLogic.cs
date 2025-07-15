using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic;

public class PooledImmichFrameLogic: LogicPoolAdapter, IAccountImmichFrameLogic
{
    private readonly IGeneralSettings _generalSettings;
    private readonly ImmichApi _immichApi;
    private readonly string _downloadLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");

    public PooledImmichFrameLogic(IGeneralSettings generalSettings, PoolConfiguration poolConfiguration)
        : base(poolConfiguration.AssetPool, poolConfiguration.ImmichApi, generalSettings.Webhook)
    {
        _generalSettings = generalSettings;
        _immichApi = poolConfiguration.ImmichApi;
    }
    
    public IAccountSettings AccountSettings { get; }
    
    public async Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id)
    {
// Check if the image is already downloaded
        if (_generalSettings.DownloadImages)
        {
            if (!Directory.Exists(_downloadLocation))
            {
                Directory.CreateDirectory(_downloadLocation);
            }

            var file = Directory.GetFiles(_downloadLocation)
                .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == id.ToString());

            if (!string.IsNullOrWhiteSpace(file))
            {
                if (_generalSettings.RenewImagesDuration > (DateTime.UtcNow - File.GetCreationTimeUtc(file)).Days)
                {
                    var fs = File.OpenRead(file);

                    var ex = Path.GetExtension(file);

                    return (Path.GetFileName(file), $"image/{ex}", fs);
                }

                File.Delete(file);
            }
        }

        var (fileName, contentType, fileStream) = await base.GetImage(id);

        if (_generalSettings.DownloadImages)
        {
            var filePath = Path.Combine(_downloadLocation, fileName);

            // save to folder
            var fs = File.Create(filePath);
            await fileStream.CopyToAsync(fs);
            fs.Position = 0;
            return (Path.GetFileName(filePath), contentType, fs);
        }

        return (fileName, contentType, fileStream);
    }

    public override string ToString() => $"Account Pool [{_immichApi.BaseUrl}]";
}