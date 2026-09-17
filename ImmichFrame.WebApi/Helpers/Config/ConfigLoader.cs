using System.Text.Json;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.WebApi.Models;
using YamlDotNet.Serialization;

namespace ImmichFrame.WebApi.Helpers.Config;

/// <summary>
/// Reads an existing configuration file so it can be imported into the database once.
/// The database is the source of truth afterwards; see <see cref="Services.SettingsService"/>.
/// </summary>
public class ConfigLoader(ILogger<ConfigLoader> _logger)
{
    private string FindConfigFile(string dir, params string[] fileNames)
    {
        if (!Directory.Exists(dir))
        {
            return Path.Combine(dir, fileNames.First());
        }

        return Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => fileNames.Any(name => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
            ?? Path.Combine(dir, fileNames.First());
    }

    internal ServerSettings LoadConfigRaw(string configPath)
    {
        var jsonConfigPath = FindConfigFile(configPath, "Settings.json");
        if (File.Exists(jsonConfigPath))
        {
            _logger.LogInformation("Loading configuration from {path}", jsonConfigPath);
            return LoadConfigJson<ServerSettings>(jsonConfigPath);
        }

        var ymlConfigPath = FindConfigFile(configPath, "Settings.yml", "Settings.yaml");
        if (File.Exists(ymlConfigPath))
        {
            _logger.LogInformation("Loading configuration from {path}", ymlConfigPath);
            return LoadConfigYaml<ServerSettings>(ymlConfigPath);
        }

        throw new ImmichFrameException("Failed to load configuration");
    }

    internal T LoadConfigJson<T>(string configPath) where T : new()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var doc = JsonDocument.Parse(json);
                return doc.Deserialize<T>() ?? throw new FileLoadException("Failed to load config file", configPath);
            }

            throw new FileNotFoundException(configPath);
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
        }
    }

    internal T LoadConfigYaml<T>(string configPath) where T : new()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var yml = File.ReadAllText(configPath);
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
                return deserializer.Deserialize<T>(yml) ?? throw new FileLoadException("Failed to load config file", configPath);
            }

            throw new FileNotFoundException(configPath);
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
        }
    }
}
