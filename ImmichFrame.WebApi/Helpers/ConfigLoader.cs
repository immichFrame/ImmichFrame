using System.Text.Json;
using ImmichFrame.Core.Exceptions;

namespace ImmichFrame.WebApi.Helpers;

public class ConfigLoader
{
    public T LoadConfig<T>(String? configPath) where T : IConfigSettable, new()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                JsonDocument doc;

                doc = JsonDocument.Parse(json);
                return JsonSerializer.Deserialize<T>(doc);
            }

            var env = Environment.GetEnvironmentVariables();
            var config = new T();

            foreach (var key in env.Keys)
            {
                if (key == null) continue;

                var propertyInfo = typeof(T).GetProperty(key.ToString() ?? string.Empty);

                if (propertyInfo != null)
                {
                    config.SetValue(propertyInfo, env[key]?.ToString() ?? string.Empty);
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
        }
    }
}