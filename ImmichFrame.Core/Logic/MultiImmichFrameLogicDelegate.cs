using System.Collections.Frozen;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.Core.Logic;

public class MultiImmichFrameLogicDelegate : IImmichFrameLogic
{
    private readonly FrozenDictionary<IAccountSettings, IImmichFrameLogic> _accountToDelegate;
    private readonly IServerSettings _serverSettings;
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IAccountSelectionStrategy _accountSelectionStrategy;
    private readonly ILogger<MultiImmichFrameLogicDelegate> _logger;
    private readonly Random _random = new();

    private readonly MemoryCacheEntryOptions _guidCacheOptions = new MemoryCacheEntryOptions()
        .SetSlidingExpiration(TimeSpan.FromSeconds(60)); //cache guids for 60sec

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
    }

    public async Task<AssetResponseDto?> GetNextAsset()
    {
        var result = await _accountSelectionStrategy.GetNextAsset(_accountToDelegate.Values);

        if (result == null) return null;

        var (account, asset) = result.Value;
        
        _cache.Set(asset.Id, account, _guidCacheOptions);

        return asset;
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets()
    {
        //get assets from all accounts, as tuple of account to asset list
        var assetLists = await _accountSelectionStrategy.GetAssets(_accountToDelegate.Values);

        foreach (var (account, assetList) in assetLists)
        {
            foreach (var asset in assetList)
            {
                _cache.Set(asset.Id, account, _guidCacheOptions);
            }
        }

        var assets = assetLists.SelectMany(innerList => innerList.Item2);

        //shuffle them together
        return assets.OrderBy(_ => _random.Next()).ToList();
    }

    public Task<AssetResponseDto> GetAssetInfoById(Guid assetId)
    {
        return GetLogic(assetId).GetAssetInfoById(assetId);
    }

    private IImmichFrameLogic GetLogic(Guid id)
    {
        var account = _cache.Get(id.ToString());

        if (account != null) return (IImmichFrameLogic)account;

        _logger.LogWarning("Failed to load asset with GUID {id}; not in cache", id);
        throw new InvalidOperationException("Unknown assetId");
    }

    public Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId)
    {
        return GetLogic(assetId).GetAlbumInfoById(assetId);
    }

    public Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id)
    {
        return GetLogic(id).GetImage(id);
    }

    public async Task<long> GetTotalAssets()
    {
        var allInts = await Task.WhenAll(_accountToDelegate.Values.Select(account => account.GetTotalAssets()));
        return allInts.Sum();
    }


    public Task SendWebhookNotification(IWebhookNotification notification) =>
        WebhookHelper.SendWebhookNotification(notification, _serverSettings.GeneralSettings.Webhook);
}