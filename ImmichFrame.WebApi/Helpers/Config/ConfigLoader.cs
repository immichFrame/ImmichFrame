using System.Collections;
using System.Text.Json;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Helpers.Config;

public class ConfigLoader(ILogger<ConfigLoader> _logger)
{
    public IServerSettings LoadConfig(string filename)
    {
        try
        {
            return LoadConfigJson<ServerSettings>(filename);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Failed to load config as current version, falling back to old version (${errorMessage})", e.Message);
        }

        try
        {
            var v1 = LoadConfigJson<ServerSettingsV1>(filename);
            return new ServerSettingsV1Adapter(v1);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Failed to load config as old JSON, falling back to env vars (${errorMessage})", e.Message);
        }

        try
        {
            var v1 = LoadConfigFromDictionary<ServerSettingsV1>(Environment.GetEnvironmentVariables());
            return new ServerSettingsV1Adapter(v1);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Failed to load config as env vars (${errorMessage})", e.Message);
        }
        
        throw new ImmichFrameException("Failed to load configuration");
    }

    internal T LoadConfigFromDictionary<T>(IDictionary env) where T : IConfigSettable, new()
    {
        var config = new T();
        var propertiesSet = 0;

        foreach (var key in env.Keys)
        {
            if (key == null) continue;

            var propertyInfo = typeof(T).GetProperty(key.ToString() ?? string.Empty);

            if (propertyInfo != null)
            {
                config.SetValue(propertyInfo, env[key]?.ToString() ?? string.Empty);
                propertiesSet++;
            }
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
}