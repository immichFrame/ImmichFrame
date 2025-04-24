using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using OpenWeatherMap;

public class OptimizedImmichFrameLogic : IImmichFrameLogic, IDisposable
{
    private readonly IServerSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ImmichApi _immichApi;
    public OptimizedImmichFrameLogic(IServerSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient();
        _httpClient.UseApiKey(_settings.ApiKey);
        _immichApi = new ImmichApi(_settings.ImmichServerUrl, _httpClient);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public Task<List<AssetResponseDto>> GetAssets()
    {
        throw new NotImplementedException();
    }

    public Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<AssetResponseDto?> GetNextAsset()
    {
        throw new NotImplementedException();
    }

    public Task SendWebhookNotification(IWebhookNotification notification) => WebhookHelper.SendWebhookNotification(notification, _settings.Webhook);
}