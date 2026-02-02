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
        var hasAlbums = accountSettings.Albums?.Any() ?? false;
        var hasPeople = accountSettings.People?.Any() ?? false;
        var hasTags = accountSettings.Tags?.Any() ?? false;

        if (!accountSettings.ShowFavorites && !accountSettings.ShowMemories && !hasAlbums && !hasPeople && !hasTags)
        {
            return new AllAssetsPool(_apiCache, _immichApi, accountSettings);
        }

        var pools = new List<IAssetPool>();

        if (accountSettings.ShowFavorites)
            pools.Add(new FavoriteAssetsPool(_apiCache, _immichApi, accountSettings));

        if (accountSettings.ShowMemories)
            pools.Add(new MemoryAssetsPool(_immichApi, accountSettings));

        if (hasAlbums)
            pools.Add(new AlbumAssetsPool(_apiCache, _immichApi, accountSettings));

        if (hasPeople)
            pools.Add(new PersonAssetsPool(_apiCache, _immichApi, accountSettings));

        if (hasTags)
            pools.Add(new TagAssetsPool(_apiCache, _immichApi, accountSettings));

        return new MultiAssetPool(pools);
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

    public async Task<(string fileName, string ContentType, Stream fileStream)> GetAsset(Guid id, AssetTypeEnum? assetType = null)
    {
        if (!assetType.HasValue)
        {
            var assetInfo = await _immichApi.GetAssetInfoAsync(id, null);
            if (assetInfo == null)
                throw new AssetNotFoundException($"Assetinfo for asset '{id}' was not found!");
            assetType = assetInfo.Type;
        }

        if (assetType == AssetTypeEnum.IMAGE)
        {
            return await GetImageAsset(id);
        }

        if (assetType == AssetTypeEnum.VIDEO)
        {
            return await GetVideoAsset(id);
        }

        throw new AssetNotFoundException($"Asset {id} is not a supported media type ({assetType}).");
    }

    private async Task<(string fileName, string ContentType, Stream fileStream)> GetImageAsset(Guid id)
    {
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

                    var ex = Path.GetExtension(file).TrimStart('.');

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

    private async Task<(string fileName, string ContentType, Stream fileStream)> GetVideoAsset(Guid id)
    {
        var fileName = $"{id}.mp4";

        if (_generalSettings.DownloadImages)
        {
            if (!Directory.Exists(_downloadLocation))
            {
                Directory.CreateDirectory(_downloadLocation);
            }

            var filePath = Path.Combine(_downloadLocation, fileName);

            if (File.Exists(filePath))
            {
                if (_generalSettings.RenewImagesDuration > (DateTime.UtcNow - File.GetCreationTimeUtc(filePath)).Days)
                {
                    return (fileName, "video/mp4", File.OpenRead(filePath));
                }
                File.Delete(filePath);
            }

            using var videoResponse = await _immichApi.PlayAssetVideoAsync(id, string.Empty);

            if (videoResponse == null)
                throw new AssetNotFoundException($"Video asset {id} was not found!");

            var contentType = videoResponse.Headers.ContainsKey("Content-Type")
                ? videoResponse.Headers["Content-Type"].FirstOrDefault() ?? "video/mp4"
                : "video/mp4";

            using (var fileStream = File.Create(filePath))
            {
                await videoResponse.Stream.CopyToAsync(fileStream);
            }

            return (fileName, contentType, File.OpenRead(filePath));
        }
        else
        {
            using var videoResponse = await _immichApi.PlayAssetVideoAsync(id, string.Empty);

            if (videoResponse == null)
                throw new AssetNotFoundException($"Video asset {id} was not found!");

            var contentType = videoResponse.Headers.ContainsKey("Content-Type")
                ? videoResponse.Headers["Content-Type"].FirstOrDefault() ?? "video/mp4"
                : "video/mp4";

            var memoryStream = new MemoryStream();
            await videoResponse.Stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return (fileName, contentType, memoryStream);
        }
    }
    public Task SendWebhookNotification(IWebhookNotification notification) =>
        WebhookHelper.SendWebhookNotification(notification, _generalSettings.Webhook);

    public override string ToString() => $"Account Pool [{_immichApi.BaseUrl}]";
}
