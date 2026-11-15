using ImmichFrame.Core.Exceptions;

namespace ImmichFrame.WebApi.Services;

/// <summary>Where runtime settings come from. Database is the default.</summary>
public enum SettingsSource
{
    /// <summary>SQLite in the config directory. A config file is imported once.</summary>
    Database,

    /// <summary>
    /// Settings.json / Settings.yml / Settings.yaml, read once at startup.
    /// The admin UI is disabled and the config directory is not written to.
    /// </summary>
    File
}

public static class SettingsSourceParser
{
    public const string EnvVar = "IMMICHFRAME_CONFIG_SOURCE";

    public static SettingsSource FromEnvironment() => Parse(Environment.GetEnvironmentVariable(EnvVar));

    public static SettingsSource Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("database", StringComparison.OrdinalIgnoreCase))
        {
            return SettingsSource.Database;
        }

        if (raw.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            return SettingsSource.File;
        }

        throw new ImmichFrameException(
            $"Unknown {EnvVar} value '{raw}'. Expected 'database' or 'file'.");
    }
}
