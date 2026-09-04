using System.Text.Json;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Database;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ImmichFrame.WebApi.Services;

public record SettingsServiceOptions(string ConfigPath);

/// <summary>
/// Owns the runtime settings. The SQLite database is the source of truth; an existing
/// file config is imported once on first run. <see cref="Current"/> always holds a
/// validated, immutable-after-publication snapshot; updates swap the reference and raise
/// <see cref="SettingsChanged"/>.
/// </summary>
public class SettingsService : ISettingsProvider
{
    private readonly IDbContextFactory<SettingsDbContext> _dbFactory;
    private readonly ConfigLoader _configLoader;
    private readonly ILogger<SettingsService> _logger;
    private readonly SettingsServiceOptions _options;
    private readonly SemaphoreSlim _updateLock = new(1, 1);

    private volatile IServerSettings _current = EmptySettings();
    // The raw (pre-Validate) form: ApiKeyFile stays unresolved so it round-trips to the admin UI and DB
    private ServerSettings _raw = EmptySettings();
    private volatile bool _isUnconfigured;

    /// <summary>
    /// This instance has never been configured: no database row, and no config file to
    /// import either. Only then may anonymous onboarding claim it. Defaults to false so
    /// an uninitialized service never opens onboarding by accident.
    /// </summary>
    public bool IsUnconfigured => _isUnconfigured;

    public SettingsService(IDbContextFactory<SettingsDbContext> dbFactory, ConfigLoader configLoader,
        ILogger<SettingsService> logger, SettingsServiceOptions options)
    {
        _dbFactory = dbFactory;
        _configLoader = configLoader;
        _logger = logger;
        _options = options;
    }

    public IServerSettings Current => _current;
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_options.ConfigPath);
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();

        var row = await db.SettingsDocuments.FindAsync(1);
        if (row != null)
        {
            LogIgnoredFileConfig();
            var raw = Deserialize(row.Json);
            _raw = raw;
            _current = ValidateOrSafeMode(raw);
            _logger.LogInformation("Loaded settings from database (last updated {updatedAt:u})", row.UpdatedAtUtc);
            return;
        }

        await ImportOrBootstrap(db);
    }

    private async Task ImportOrBootstrap(SettingsDbContext db)
    {
        ServerSettings raw;
        try
        {
            raw = _configLoader.LoadConfigRaw(_options.ConfigPath);
        }
        catch (ImmichFrameException)
        {
            _logger.LogWarning("No configuration found (file or database). Starting with defaults — use the admin UI to configure ImmichFrame.");
            _raw = EmptySettings();
            _current = _raw;
            _isUnconfigured = true;
            return;
        }

        Normalize(raw);

        // Only lock the config into the DB if it is actually valid; otherwise the old
        // fix-the-file-and-restart workflow must keep working.
        var validated = Clone(raw);
        try
        {
            validated.Validate();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Existing configuration is invalid and was not imported: {message}. Fix the configuration and restart.", ex.Message);
            _raw = raw;
            _current = ValidateOrSafeMode(raw);
            return;
        }

        db.SettingsDocuments.Add(new SettingsDocument
        {
            Id = 1,
            Json = Serialize(raw),
            UpdatedAtUtc = DateTime.UtcNow,
            ImportedFrom = DetectImportSource(),
            Version = 1
        });
        await db.SaveChangesAsync();

        _raw = raw;
        _current = validated;
        var source = DetectImportSource();
        _logger.LogInformation("Imported existing configuration ({source}) into the database. The database is now the source of truth; changes to the old config are ignored.", source);
        _logger.LogWarning("DEPRECATED: configuring ImmichFrame via {source} will be removed in a future version. The import above runs once — manage your settings in the admin UI at /admin from now on.", source);
    }

    public async Task<IServerSettings> UpdateAsync(ServerSettings raw)
    {
        Normalize(raw);
        var validated = Clone(raw);
        try
        {
            validated.Validate();
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException(ex.Message, ex);
        }

        await _updateLock.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var row = await db.SettingsDocuments.FindAsync(1);
            if (row == null)
            {
                row = new SettingsDocument { Id = 1 };
                db.SettingsDocuments.Add(row);
            }

            row.Json = Serialize(raw);
            row.UpdatedAtUtc = DateTime.UtcNow;
            row.Version++;
            await db.SaveChangesAsync();

            var old = _raw;
            var accountsChanged = Serialize(old.AccountsImpl) != Serialize(raw.AccountsImpl)
                || old.GeneralSettings.RefreshAlbumPeopleInterval != validated.GeneralSettings.RefreshAlbumPeopleInterval;
            var generalChanged = Serialize(old.GeneralSettingsImpl) != Serialize(raw.GeneralSettingsImpl);

            _raw = Clone(raw);
            _current = validated;
            _isUnconfigured = false;

            _logger.LogInformation("Settings updated (accounts changed: {accountsChanged})", accountsChanged);
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs
            {
                NewSettings = validated,
                AccountsChanged = accountsChanged,
                GeneralChanged = generalChanged
            });

            return validated;
        }
        finally
        {
            _updateLock.Release();
        }
    }

    /// <summary>The raw settings for editing: secrets included, ApiKeyFile unresolved.</summary>
    public ServerSettings GetRawSettings() => Clone(_raw);

    private ServerSettings ValidateOrSafeMode(ServerSettings raw)
    {
        var clone = Clone(raw);
        try
        {
            clone.Validate();
            return clone;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Stored settings failed validation: {message}. Starting with the valid subset — fix the configuration via the admin UI.", ex.Message);
        }

        var safe = Clone(raw);
        var validAccounts = new List<ServerAccountSettings>();
        foreach (var account in safe.AccountsImpl)
        {
            try
            {
                account.ValidateAndInitialize();
                validAccounts.Add(account);
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Skipping invalid account '{url}': {message}", account.ImmichServerUrl, ex.Message);
            }
        }

        safe.AccountsImpl = validAccounts;
        return safe;
    }

    private void LogIgnoredFileConfig()
    {
        var ignored = new[] { "Settings.json", "Settings.yml", "Settings.yaml" }
            .Where(f => File.Exists(Path.Combine(_options.ConfigPath, f)))
            .ToList();
        if (ignored.Any())
        {
            _logger.LogInformation("Config file(s) {files} exist but are ignored: settings were already imported into the database, which is the source of truth. Use the admin UI to change settings.", string.Join(", ", ignored));
        }
    }

    private string DetectImportSource()
    {
        if (File.Exists(Path.Combine(_options.ConfigPath, "Settings.json"))) return "Settings.json";
        if (File.Exists(Path.Combine(_options.ConfigPath, "Settings.yml"))) return "Settings.yml";
        if (File.Exists(Path.Combine(_options.ConfigPath, "Settings.yaml"))) return "Settings.yaml";
        return "an unknown source";
    }

    private static ServerSettings EmptySettings() => new()
    {
        GeneralSettingsImpl = new GeneralSettings(),
        AccountsImpl = new List<ServerAccountSettings>()
    };

    private static void Normalize(ServerSettings settings)
    {
        settings.GeneralSettingsImpl ??= new GeneralSettings();
        settings.AccountsImpl ??= new List<ServerAccountSettings>();
    }

    internal static string Serialize(object? value) => JsonSerializer.Serialize(value);

    private static ServerSettings Deserialize(string json)
    {
        var settings = JsonSerializer.Deserialize<ServerSettings>(json)
            ?? throw new SettingsNotValidException("Stored settings could not be parsed");
        Normalize(settings);
        return settings;
    }

    private static ServerSettings Clone(ServerSettings settings)
    {
        return Deserialize(JsonSerializer.Serialize(settings));
    }
}
