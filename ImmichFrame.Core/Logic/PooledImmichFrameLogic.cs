using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Logic;

public class PooledImmichFrameLogic : IAccountImmichFrameLogic
{
    private readonly IGeneralSettings _generalSettings;
    private readonly IApiCache _apiCache;
    private readonly IAssetPool _pool;
    private readonly ImmichApi _immichApi;
    private readonly string _downloadLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");

    public PooledImmichFrameLogic(IAccountSettings accountSettings, IGeneralSettings generalSettings, IHttpClientFactory httpClientFactory)
    {
        _generalSettings = generalSettings;

        var httpClient = httpClientFactory.CreateClient("ImmichApiAccountClient");
        AccountSettings = accountSettings;

        httpClient.UseApiKey(accountSettings.ApiKey);
        _immichApi = new ImmichApi(accountSettings.ImmichServerUrl, httpClient);

        _apiCache = new ApiCache(RefreshInterval(generalSettings.RefreshAlbumPeopleInterval));
        _pool = BuildPool(accountSettings);
    }

    private static TimeSpan RefreshInterval(int hours)
        => hours > 0 ? TimeSpan.FromHours(hours) : TimeSpan.FromMilliseconds(1);

    public IAccountSettings AccountSettings { get; }

    private IAssetPool BuildPool(IAccountSettings accountSettings)
    {
        IAssetPool basePool;
        
        if (!accountSettings.ShowFavorites && !accountSettings.ShowMemories && !accountSettings.Albums.Any() && !accountSettings.People.Any())
        {
            basePool = new AllAssetsPool(_apiCache, _immichApi, accountSettings);
        }
        else
        {
            var pools = new List<IAssetPool>();

            if (accountSettings.ShowFavorites)
                pools.Add(new FavoriteAssetsPool(_apiCache, _immichApi, accountSettings));

            if (accountSettings.ShowMemories)
                pools.Add(new MemoryAssetsPool(_immichApi, accountSettings));

            if (accountSettings.Albums.Any())
                pools.Add(new AlbumAssetsPool(_apiCache, _immichApi, accountSettings));

            if (accountSettings.People.Any())
                pools.Add(new PersonAssetsPool(_apiCache, _immichApi, accountSettings));

            basePool = new MultiAssetPool(pools);
        }
        
        // Wrap with chronological logic if enabled
        if (_generalSettings.ShowChronologicalImages)
        {
            return new ChronologicalAssetsPoolWrapper(basePool, _generalSettings);
        }
        
        return basePool;
    }

    public async Task<AssetResponseDto?> GetNextAsset()
    {
        return (await _pool.GetAssets(1)).FirstOrDefault();
    }

    public Task<IEnumerable<AssetResponseDto>> GetAssets()
    {
        return _pool.GetAssets(25);
    }

    public Task<AssetResponseDto> GetAssetInfoById(Guid assetId) => _immichApi.GetAssetInfoAsync(assetId, null);

    public async Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId) => await _immichApi.GetAllAlbumsAsync(assetId, null);

    public Task<long> GetTotalAssets() => _pool.GetAssetCount();

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

        var data = await _immichApi.ViewAssetAsync(id, string.Empty, AssetMediaSize.Preview);

        if (data == null)
            throw new AssetNotFoundException($"Asset {id} was not found!");

        var contentType = "";
        if (data.Headers.ContainsKey("Content-Type"))
        {
            contentType = data.Headers["Content-Type"].FirstOrDefault() ?? "";
        }

        var ext = contentType.ToLower() == "image/webp" ? "webp" : "jpeg";
        var fileName = $"{id}.{ext}";

        if (_generalSettings.DownloadImages)
        {
            var stream = data.Stream;

            var filePath = Path.Combine(_downloadLocation, fileName);

            // save to folder
            var fs = File.Create(filePath);
            await stream.CopyToAsync(fs);
            fs.Position = 0;
            return (Path.GetFileName(filePath), contentType, fs);
        }

        return (fileName, contentType, data.Stream);
    }

    public Task SendWebhookNotification(IWebhookNotification notification) =>
        WebhookHelper.SendWebhookNotification(notification, _generalSettings.Webhook);

    public override string ToString() => $"Account Pool [{_immichApi.BaseUrl}]";
}