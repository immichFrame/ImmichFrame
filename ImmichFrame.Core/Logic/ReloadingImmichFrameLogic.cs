using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic;

/// <summary>
/// Delegates to an inner <see cref="IImmichFrameLogic"/> that is rebuilt whenever the
/// account settings change, so account changes apply without a restart. The previous
/// instance is disposed after a grace delay to let in-flight requests finish.
/// </summary>
public class ReloadingImmichFrameLogic : IImmichFrameLogic, IDisposable
{
    private static readonly TimeSpan DefaultDisposeGraceDelay = TimeSpan.FromSeconds(60);

    private readonly Func<IImmichFrameLogic> _innerFactory;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ILogger<ReloadingImmichFrameLogic> _logger;
    private readonly TimeSpan _disposeGraceDelay;
    private volatile IImmichFrameLogic _inner;

    public ReloadingImmichFrameLogic(ISettingsProvider settingsProvider, Func<IImmichFrameLogic> innerFactory,
        ILogger<ReloadingImmichFrameLogic> logger, TimeSpan? disposeGraceDelay = null)
    {
        _settingsProvider = settingsProvider;
        _innerFactory = innerFactory;
        _logger = logger;
        _disposeGraceDelay = disposeGraceDelay ?? DefaultDisposeGraceDelay;
        _inner = innerFactory();
        _settingsProvider.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs args)
    {
        if (!args.AccountsChanged)
            return;

        _logger.LogInformation("Account settings changed, rebuilding asset logic");
        var next = _innerFactory();
        var old = Interlocked.Exchange(ref _inner, next);
        if (old is IDisposable disposable)
        {
            var delay = _disposeGraceDelay;
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay);
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispose previous asset logic");
                }
            });
        }
    }

    public Task<AssetResponseDto?> GetNextAsset() => _inner.GetNextAsset();

    public Task<IEnumerable<AssetResponseDto>> GetAssets() => _inner.GetAssets();

    public Task<AssetResponseDto> GetAssetInfoById(Guid assetId) => _inner.GetAssetInfoById(assetId);

    public Task<IEnumerable<AssetFaceResponseDto>> GetAssetFacesById(Guid assetId) => _inner.GetAssetFacesById(assetId);

    public Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId) => _inner.GetAlbumInfoById(assetId);

    public Task<AssetResponse> GetAsset(Guid assetId, AssetTypeEnum? assetType = null, string? rangeHeader = null)
        => _inner.GetAsset(assetId, assetType, rangeHeader);

    public Task<long> GetTotalAssets() => _inner.GetTotalAssets();

    public Task SendWebhookNotification(IWebhookNotification notification) => _inner.SendWebhookNotification(notification);

    public void Dispose()
    {
        _settingsProvider.SettingsChanged -= OnSettingsChanged;
        (_inner as IDisposable)?.Dispose();
    }
}
