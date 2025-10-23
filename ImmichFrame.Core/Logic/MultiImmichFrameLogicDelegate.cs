using System.Collections.Frozen;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Logic;

public class MultiImmichFrameLogicDelegate : IImmichFrameLogic
{
    private readonly FrozenDictionary<IAccountSettings, IAccountImmichFrameLogic> _accountToDelegate;
    private readonly IServerSettings _serverSettings;
    private readonly IAccountSelectionStrategy _accountSelectionStrategy;
    private readonly ILogger<MultiImmichFrameLogicDelegate> _logger;

    public MultiImmichFrameLogicDelegate(IServerSettings serverSettings,
        Func<IAccountSettings, IAccountImmichFrameLogic> logicFactory, ILogger<MultiImmichFrameLogicDelegate> logger,
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

    public async Task<AssetResponseDto?> GetNextAsset() => (await _accountSelectionStrategy.GetNextAsset())?.ToAsset();


    public async Task<IEnumerable<AssetResponseDto>> GetAssets()
       // Preserve asset order from selection strategy (required for chronological grouping, no shuffling here)
        => (await _accountSelectionStrategy.GetAssets()).Select(it => it.ToAsset());


    public Task<AssetResponseDto> GetAssetInfoById(Guid assetId)
        => _accountSelectionStrategy.ForAsset(assetId, async logic => (await logic.GetAssetInfoById(assetId)).WithAccount(logic));


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

public static class AccountAndAssetExtensions
{
    public static AssetResponseDto ToAsset(this (IAccountImmichFrameLogic, AssetResponseDto) accountAndAsset)
    {
        var (account, asset) = accountAndAsset;
        return asset.WithAccount(account);
    }

    public static AssetResponseDto WithAccount(this AssetResponseDto asset, IAccountImmichFrameLogic account)
    {
        asset.ImmichServerUrl = account.AccountSettings.ImmichServerUrl;
        return asset;
    }
}