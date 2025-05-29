using System.Collections.Frozen;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.Core.Logic;

public class MultiImmichFrameLogicDelegate : IImmichFrameLogic
{
    private readonly FrozenDictionary<IImmichAccountSettings, IImmichFrameLogic> _accountToDelegate;
    private readonly IServerSettings _serverSettings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MultiImmichFrameLogicDelegate> _logger;
    
    private readonly MemoryCacheEntryOptions _guidCacheOptions = new MemoryCacheEntryOptions()
        .SetSlidingExpiration(TimeSpan.FromSeconds(60)); //cache guids for 60sec

    public MultiImmichFrameLogicDelegate(IServerSettings serverSettings,
        Func<IImmichAccountSettings, IImmichFrameLogic> logicFactory, ILogger<MultiImmichFrameLogicDelegate> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serverSettings = serverSettings;
        _accountToDelegate = serverSettings.Accounts.ToFrozenDictionary(
            keySelector: a => a,
            elementSelector: logicFactory
        );

        var cacheOptions = new MemoryCacheOptions();
        cacheOptions.ExpirationScanFrequency = TimeSpan.MaxValue;
        _cache = new MemoryCache(cacheOptions);
    }
    
    public async Task<AssetResponseDto?> GetNextAsset()
    {
        var selectedIdx = new Random().Next(_accountToDelegate.Count);
        var account = _serverSettings.Accounts[selectedIdx];
        var asset = await _accountToDelegate[account].GetNextAsset();
        
        if (asset != null)
        {
            _cache.GetOrCreate(asset.Id, entry => entry.Value = account, _guidCacheOptions);
        }

        return asset;
    }

    public async Task<IEnumerable<AssetResponseDto>> GetAssets()
    {
        //get assets from all accounts, as tuple of account to asset list
        var assetLists = await Task.WhenAll(_accountToDelegate.Values.Select(account => account.GetAssets().ContinueWith(async assetList => (account, await assetList)).Unwrap()));

        foreach (var (account, assetList) in assetLists)
        {
            foreach (var asset in assetList)
            {
                _cache.GetOrCreate(asset.Id, entry => entry.Value = account, _guidCacheOptions);
            }
        }
        
        var assets = assetLists.SelectMany(innerList => innerList.Item2);

        //shuffle them together
        return assets.OrderBy(_ => Guid.NewGuid()).ToList();
    }

    public Task<AssetResponseDto> GetAssetInfoById(Guid assetId)
    {
        return GetLogic(assetId).GetAssetInfoById(assetId);
    }

    private IImmichFrameLogic GetLogic(Guid id)
    {
        var found = _cache.TryGetValue(id.ToString(), out IImmichAccountSettings? account);

        if (found && account != null) return _accountToDelegate[account];

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

    public Task SendWebhookNotification(IWebhookNotification notification) =>
        WebhookHelper.SendWebhookNotification(notification, _serverSettings.ImmichFrameSettings.Webhook);
}