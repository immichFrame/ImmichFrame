using System.Collections.Frozen;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.Core.Logic;

public class MultiImmichFrameLogicDelegate : IImmichFrameLogic
{
    private readonly FrozenDictionary<IAccountSettings, IImmichFrameLogic> _accountToDelegate;
    private readonly IServerSettings _serverSettings;
    private readonly IAccountSelectionStrategy _accountSelectionStrategy;
    private readonly ILogger<MultiImmichFrameLogicDelegate> _logger;

    public MultiImmichFrameLogicDelegate(IServerSettings serverSettings,
        Func<IAccountSettings, IImmichFrameLogic> logicFactory, ILogger<MultiImmichFrameLogicDelegate> logger,
        IAccountSelectionStrategy accountSelectionStrategy)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _accountSelectionStrategy = accountSelectionStrategy;
        _serverSettings = serverSettings;
        _accountToDelegate = serverSettings.Accounts.ToFrozenDictionary(
            keySelector: a => a,
            elementSelector: logicFactory
        );
        _accountSelectionStrategy.Initialize(_accountToDelegate.Values);
    }

    public Task<AssetResponseDto?> GetNextAsset() => _accountSelectionStrategy.GetNextAsset();


    public async Task<IEnumerable<AssetResponseDto>> GetAssets()
        => (await _accountSelectionStrategy.GetAssets()).Shuffle().ToList();


    public Task<AssetResponseDto> GetAssetInfoById(Guid assetId)
        => _accountSelectionStrategy.ForAsset(assetId, logic => logic.GetAssetInfoById(assetId));


    public Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId)
        => _accountSelectionStrategy.ForAsset(assetId, logic => logic.GetAlbumInfoById(assetId));


    public Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid assetId)
        => _accountSelectionStrategy.ForAsset(assetId, logic => logic.GetImage(assetId));

    public async Task<long> GetTotalAssets()
    {
        var allInts = await Task.WhenAll(_accountToDelegate.Values.Select(account => account.GetTotalAssets()));
        return allInts.Sum();
    }

    public Task SendWebhookNotification(IWebhookNotification notification) =>
        WebhookHelper.SendWebhookNotification(notification, _serverSettings.GeneralSettings.Webhook);
}