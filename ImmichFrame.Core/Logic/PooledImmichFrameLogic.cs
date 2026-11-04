using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;
using ImmichFrame.Core.Models;

namespace ImmichFrame.Core.Logic;

public class PooledImmichFrameLogic : IAccountImmichFrameLogic, IDisposable
{
    private const int BatchSize = 25;

    private readonly IGeneralSettings _generalSettings;
    private readonly IApiCache _apiCache;
    private readonly IAssetPool _pool;
    private readonly ImmichApi _immichApi;
    private readonly List<IDisposable> _disposables = new();
    private readonly string _downloadLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");

    public PooledImmichFrameLogic(IAccountSettings accountSettings, IGeneralSettings generalSettings, IHttpClientFactory httpClientFactory)
    {
        _generalSettings = generalSettings;

        var httpClient = httpClientFactory.CreateClient(ImmichApiHttpClientExtensions.ImmichApiAccountClient);
        AccountSettings = accountSettings;

        httpClient.UseApiKey(accountSettings.ApiKey);
        _immichApi = new ImmichApi(accountSettings.ImmichServerUrl, httpClient);

        var refreshInterval = RefreshInterval(generalSettings.RefreshAlbumPeopleInterval);
        _apiCache = new ApiCache(refreshInterval);
        _pool = BuildPool(accountSettings, refreshInterval);
    }

    private static TimeSpan RefreshInterval(int hours)
        => hours > 0 ? TimeSpan.FromHours(hours) : TimeSpan.FromMilliseconds(1);

    public IAccountSettings AccountSettings { get; }

    private IAssetPool BuildPool(IAccountSettings accountSettings, TimeSpan refreshInterval)
    {
        var hasSource = accountSettings.ShowFavorites
            || (accountSettings.Albums?.Any() ?? false)
            || (accountSettings.People?.Any() ?? false)
            || (accountSettings.Tags?.Any() ?? false);

        var pools = new List<IAssetPool>();

        // Memories-only skips the library search. Every other case needs it.
        if (!accountSettings.ShowMemories || hasSource)
        {
            // The ID list is only used in Exhaustive mode, which keeps it at least AccountSearchPool.MinIdListLifetime.
            var searchPool = new AccountSearchPool(_apiCache, _immichApi, accountSettings, refreshInterval);
            _disposables.Add(searchPool);
            pools.Add(ApplyShuffleMode(searchPool, accountSettings.ShuffleMode));
        }

        if (accountSettings.ShowMemories)
        {
            pools.Add(ApplyShuffleMode(new MemoryAssetsPool(_immichApi, accountSettings), accountSettings.ShuffleMode));
        }

        return pools.Count == 1 ? pools[0] : new MultiAssetPool(pools);
    }

    // Exhaustive mode deals each source from its own deck; Random mode keeps the source as it is.
    internal static IAssetPool ApplyShuffleMode(IDealableAssetPool pool, ShuffleMode mode)
        => mode == ShuffleMode.Exhaustive ? new ShuffleDeckPool(pool) : pool;

    public async Task<AssetResponseDto?> GetNextAsset(string clientId = "", CancellationToken ct = default)
    {
        return (await _pool.GetAssets(1, clientId, ct)).FirstOrDefault();
    }

    public Task<IEnumerable<AssetResponseDto>> GetAssets(string clientId = "", CancellationToken ct = default) => GetAssets(BatchSize, clientId, ct);

    public async Task<IEnumerable<AssetResponseDto>> GetAssets(int count, string clientId, CancellationToken ct = default)
    {
        return await _pool.GetAssets(count, clientId, ct);
    }

    public async Task<AssetResponseDto> GetAssetInfoById(Guid assetId) => await _immichApi.GetAssetInfoAsync(assetId, null, null);

    public async Task<IEnumerable<AssetFaceResponseDto>> GetAssetFacesById(Guid assetId) => await _immichApi.GetFacesAsync(assetId);

    public async Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId) => await _immichApi.GetAllAlbumsAsync(assetId, null, null, null, null);

    public async Task<long> GetTotalAssets() => await _pool.GetAssetCount();

    public async Task<AssetResponse> GetAsset(Guid id, AssetTypeEnum? assetType = null, string? rangeHeader = null)
    {
        if (!assetType.HasValue)
        {
            var assetInfo = await _immichApi.GetAssetInfoAsync(id, null, null);
            if (assetInfo == null)
                throw new AssetNotFoundException($"Assetinfo for asset '{id}' was not found!");
            assetType = assetInfo.Type;
        }

        if (assetType == AssetTypeEnum.IMAGE)
        {
            var (fileName, contentType, fileStream) = await GetImageAsset(id);
            return new AssetResponse
            {
                FileName = fileName,
                ContentType = contentType,
                FileStream = fileStream,
                ContentRange = null,
                IsPartial = false,
                Owner = null,
                ContentLength = null
            };
        }

        if (assetType == AssetTypeEnum.VIDEO)
        {
            return await GetVideoAsset(id, rangeHeader);
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

        var data = await _immichApi.ViewAssetAsync(null, id, string.Empty, AssetMediaSize.Preview, null);

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

    private async Task<AssetResponse> GetVideoAsset(Guid id, string? rangeHeader = null)
    {
        var videoResponse = string.IsNullOrEmpty(rangeHeader)
            ? await _immichApi.PlayAssetVideoAsync(id, null, null)
            : await _immichApi.PlayAssetVideoWithRangeAsync(id, rangeHeader);

        var contentType = videoResponse.Headers.TryGetValue("Content-Type", out var ct)
            ? ct.FirstOrDefault() ?? "video/mp4"
            : "video/mp4";

        var contentRange = videoResponse.Headers.TryGetValue("Content-Range", out var cr)
            ? cr.FirstOrDefault()
            : null;

        long? contentLength = videoResponse.Headers.TryGetValue("Content-Length", out var cl)
            && long.TryParse(cl.FirstOrDefault(), out var clValue) ? clValue : null;

        return new AssetResponse
        {
            FileName = $"{id}.mp4",
            ContentType = contentType,
            FileStream = videoResponse.Stream,
            ContentRange = contentRange,
            IsPartial = videoResponse.StatusCode == 206,
            Owner = videoResponse,
            ContentLength = contentLength
        };
    }
    public async Task SendWebhookNotification(IWebhookNotification notification) =>
        await WebhookHelper.SendWebhookNotification(notification, _generalSettings.Webhook);

    public override string ToString() => $"Account Pool [{_immichApi.BaseUrl}]";

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        (_apiCache as IDisposable)?.Dispose();
    }
}
