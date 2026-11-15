using System.Text.Json;
using System.Text.Json.Serialization;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.WebApi.Models;
using YamlDotNet.Serialization;

namespace ImmichFrame.WebApi.Helpers.Config;

/// <summary>
/// Reads Settings.json / Settings.yml / Settings.yaml for <see cref="Services.SettingsService"/>.
/// Database mode imports the first file once and ignores unknown keys. File mode requires
/// exactly one file and rejects unknown keys.
/// </summary>
public class ConfigLoader(ILogger<ConfigLoader> _logger)
{
    internal static readonly string[] SettingsFileNames = ["Settings.json", "Settings.yml", "Settings.yaml"];

    /// <summary>
    /// Settings files in the config directory, in priority order. Case-insensitive, but not
    /// in filesystem enumeration order: json, then yml, then yaml.
    /// </summary>
    internal IReadOnlyList<string> ListSettingsFiles(string configPath)
    {
        if (!Directory.Exists(configPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(configPath, "*", SearchOption.TopDirectoryOnly)
            .Select(path => (path, name: Path.GetFileName(path)))
            .Where(file => SettingsFileNames.Any(expected =>
                string.Equals(file.name, expected, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(file => Array.FindIndex(SettingsFileNames, expected =>
                string.Equals(file.name, expected, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(file => file.name, StringComparer.Ordinal)
            .Select(file => file.path)
            .ToList();
    }

    /// <summary>
    /// Lenient load for the one-time database import. The first file wins; unknown keys are ignored.
    /// </summary>
    internal ServerSettings LoadConfigRaw(string configPath)
    {
        var files = ListSettingsFiles(configPath);
        if (files.Count == 0)
        {
            throw new ImmichFrameException("Failed to load configuration");
        }

        if (files.Count > 1)
        {
            _logger.LogWarning(
                "Multiple settings files found in {path}: {files}. Importing {chosen}; the others are ignored.",
                configPath,
                string.Join(", ", files.Select(Path.GetFileName)),
                Path.GetFileName(files[0]));
        }

        _logger.LogInformation("Loading configuration from {path}", files[0]);
        return LoadSettingsFile(files[0], strict: false);
    }

    internal ServerSettings LoadSettingsFile(string path, bool strict)
    {
        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return LoadConfigJson<ServerSettings>(path, strict);
        }

        return LoadConfigYaml<ServerSettings>(path, strict);
    }

    internal T LoadConfigJson<T>(string configPath, bool strict = false) where T : new()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                var options = new JsonSerializerOptions
                {
                    UnmappedMemberHandling = strict
                        ? JsonUnmappedMemberHandling.Disallow
                        : JsonUnmappedMemberHandling.Skip
                };
                return doc.Deserialize<T>(options) ?? throw new FileLoadException("Failed to load config file", configPath);
            }

            throw new FileNotFoundException(configPath);
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
        }
    }

    internal T LoadConfigYaml<T>(string configPath, bool strict = false) where T : new()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var yml = File.ReadAllText(configPath);
                var builder = new DeserializerBuilder();
                if (!strict)
                {
                    builder.IgnoreUnmatchedProperties();
                }

                return builder.Build().Deserialize<T>(yml) ?? throw new FileLoadException("Failed to load config file", configPath);
            }

            throw new FileNotFoundException(configPath);
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
        }
    }
}
