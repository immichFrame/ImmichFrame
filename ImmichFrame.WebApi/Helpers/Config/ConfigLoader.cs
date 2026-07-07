using System.Collections;
using System.Text.Json;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using YamlDotNet.Serialization;

namespace ImmichFrame.WebApi.Helpers.Config;

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
    public IServerSettings LoadConfig(string configPath)
    {
        var config = LoadConfigRaw(configPath);
        ApplyEnvironmentVariables(config);
        config.Validate();
        return config;
    }
    private IServerSettings LoadConfigRaw(string configPath)
    {
        var jsonConfigPath = FindConfigFile(configPath, "Settings.json");
        if (File.Exists(jsonConfigPath))
        {
            try
            {
                return LoadConfigJson<ServerSettings>(jsonConfigPath);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to load config as current version JSON. ({errorMessage})", e.Message);
            }

            try
            {
                var v1 = LoadConfigJson<ServerSettingsV1>(jsonConfigPath);
                return new ServerSettingsV1Adapter(v1);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to load config as old JSON. ({errorMessage})", e.Message);
            }
        }

        var ymlConfigPath = FindConfigFile(configPath, "Settings.yml", "Settings.yaml");
        if (File.Exists(ymlConfigPath))
        {
            try
            {
                return LoadConfigYaml<ServerSettings>(ymlConfigPath);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to load config as current version YAML. ({errorMessage})", e.Message);
            }

            try
            {
                var v1 = LoadConfigYaml<ServerSettingsV1>(ymlConfigPath);
                return new ServerSettingsV1Adapter(v1);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to load config as old YAML. ({errorMessage})", e.Message);
            }
        }

        try
        {
            var v1 = LoadConfigFromDictionary<ServerSettingsV1>(Environment.GetEnvironmentVariables());
            return new ServerSettingsV1Adapter(v1);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Failed to load config as env vars ({errorMessage})", e.Message);
        }

        throw new ImmichFrameException("Failed to load configuration");
    }
    private void ApplyEnvironmentVariables(IServerSettings config)
    {
        var env = Environment.GetEnvironmentVariables();
        if (config is ServerSettings serverSettings)
        {
            if (serverSettings.GeneralSettingsImpl == null)
                serverSettings.GeneralSettingsImpl = new GeneralSettings();

            MapDictionaryToConfig(serverSettings.GeneralSettingsImpl, env);
        }
        else if (config is ServerSettingsV1Adapter v1Adapter)
        {
            MapDictionaryToConfig(v1Adapter.Settings, env);
        }
    }
    internal void MapDictionaryToConfig<T>(T config, IDictionary env) where T : IConfigSettable
    {
        foreach (var key in env.Keys)
        {
            if (key == null) continue;

            var propertyInfo = typeof(T).GetProperty(key.ToString() ?? string.Empty);

            if (propertyInfo != null)
            {
                var value = env[key]?.ToString() ?? string.Empty;
                // Clean up quotes if present
                if (value.StartsWith("'") && value.EndsWith("'"))
                    value = value.Substring(1, value.Length - 2);
                if (value.StartsWith("\"") && value.EndsWith("\""))
                    value = value.Substring(1, value.Length - 2);

                config.SetValue(propertyInfo, value);
            }
        }
    }
    internal T LoadConfigFromDictionary<T>(IDictionary env) where T : IConfigSettable, new()
    {
        var config = new T();
        MapDictionaryToConfig(config, env);

        // Count set properties to see if we have anything
        var propertiesSet = 0;
        foreach (var key in env.Keys)
        {
            if (key == null) continue;
            if (typeof(T).GetProperty(key.ToString() ?? string.Empty) != null)
                propertiesSet++;
        }

        if (propertiesSet < 2)
        {
            throw new ImmichFrameException("No environment variables found");
        }

        return config;
    }

    internal T LoadConfigJson<T>(string configPath) where T : IConfigSettable, new()
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
    internal T LoadConfigYaml<T>(string configPath) where T : IConfigSettable, new()
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
