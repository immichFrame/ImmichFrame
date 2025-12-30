using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using ImmichFrame.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.WebApi.Services;

/// <summary>
/// Rebuilds the underlying MultiImmichFrameLogicDelegate when the SQLite override "version" changes,
/// so account filter changes apply without a restart.
/// </summary>
public sealed class ReloadingImmichFrameLogic : IImmichFrameLogic
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServerSettings _baseSettings;
    private readonly IAccountOverrideStore _overrideStore;
    private readonly ILogger<ReloadingImmichFrameLogic> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    private long _currentVersion = -1;
    private IImmichFrameLogic? _current;

    public ReloadingImmichFrameLogic(
        IServiceScopeFactory scopeFactory,
        IServerSettings baseSettings,
        IAccountOverrideStore overrideStore,
        ILogger<ReloadingImmichFrameLogic> logger)
    {
        _scopeFactory = scopeFactory;
        _baseSettings = baseSettings;
        _overrideStore = overrideStore;
        _logger = logger;
    }

    private async Task<IImmichFrameLogic> GetCurrentAsync(CancellationToken ct = default)
    {
        var version = await _overrideStore.GetVersionAsync(ct);
        var existing = Volatile.Read(ref _current);
        if (existing != null && version == Volatile.Read(ref _currentVersion))
        {
            return existing;
        }

        await _reloadLock.WaitAsync(ct);
        try
        {
            existing = Volatile.Read(ref _current);
            if (existing != null && version == Volatile.Read(ref _currentVersion))
            {
                return existing;
            }

            _logger.LogInformation("Rebuilding ImmichFrame logic due to override version change ({oldVersion} -> {newVersion})",
                _currentVersion, version);

            var rebuilt = await BuildAsync(ct);
            Volatile.Write(ref _current, rebuilt);
            Volatile.Write(ref _currentVersion, version);
            return rebuilt;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private async Task<IImmichFrameLogic> BuildAsync(CancellationToken ct)
    {
        var overrides = await _overrideStore.GetAsync(ct);
        var mergedSettings = BuildMergedSettings(overrides);

        using var scope = _scopeFactory.CreateScope();
        var logicFactory = scope.ServiceProvider.GetRequiredService<Func<IAccountSettings, IAccountImmichFrameLogic>>();
        var strategy = scope.ServiceProvider.GetRequiredService<IAccountSelectionStrategy>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MultiImmichFrameLogicDelegate>>();

        return new MultiImmichFrameLogicDelegate(mergedSettings, logicFactory, logger, strategy);
    }

    private IServerSettings BuildMergedSettings(AccountOverrideDto? overrides)
    {
        // Clone base accounts into new mutable instances so pools rebuild cleanly.
        var clonedAccounts = _baseSettings.Accounts
            .Select(CloneAccount)
            .Cast<IAccountSettings>()
            .ToList();

        if (overrides != null)
        {
            foreach (var account in clonedAccounts)
            {
                AccountOverrideMerger.Apply(account, overrides);
            }
        }

        var snapshot = new MergedServerSettingsSnapshot
        {
            GeneralSettings = _baseSettings.GeneralSettings,
            Accounts = clonedAccounts
        };

        snapshot.Validate();
        return snapshot;
    }

    private static ServerAccountSettings CloneAccount(IAccountSettings baseAccount) => new()
    {
        ImmichServerUrl = baseAccount.ImmichServerUrl,
        ApiKey = baseAccount.ApiKey,
        ApiKeyFile = null, // avoid re-reading file and avoid ApiKey+ApiKeyFile conflict
        ShowMemories = baseAccount.ShowMemories,
        ShowFavorites = baseAccount.ShowFavorites,
        ShowArchived = baseAccount.ShowArchived,
        ImagesFromDays = baseAccount.ImagesFromDays,
        ImagesFromDate = baseAccount.ImagesFromDate,
        ImagesUntilDate = baseAccount.ImagesUntilDate,
        Albums = baseAccount.Albums.ToList(),
        ExcludedAlbums = baseAccount.ExcludedAlbums.ToList(),
        People = baseAccount.People.ToList(),
        Rating = baseAccount.Rating
    };

    public async Task<AssetResponseDto?> GetNextAsset()
        => await (await GetCurrentAsync()).GetNextAsset();

    public async Task<IEnumerable<AssetResponseDto>> GetAssets()
        => await (await GetCurrentAsync()).GetAssets();

    public async Task<AssetResponseDto> GetAssetInfoById(Guid assetId)
        => await (await GetCurrentAsync()).GetAssetInfoById(assetId);

    public async Task<IEnumerable<AlbumResponseDto>> GetAlbumInfoById(Guid assetId)
    {
        var current = await GetCurrentAsync();
        return await current.GetAlbumInfoById(assetId);
    }

    public async Task<(string fileName, string ContentType, Stream fileStream)> GetImage(Guid id)
        => await (await GetCurrentAsync()).GetImage(id);

    public async Task<long> GetTotalAssets()
        => await (await GetCurrentAsync()).GetTotalAssets();

    public async Task SendWebhookNotification(IWebhookNotification notification)
        => await (await GetCurrentAsync()).SendWebhookNotification(notification);
}


