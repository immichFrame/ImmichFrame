using System.Text.Json;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.WebApi.Models;
using ImmichFrame.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ImmichFrame.WebApi.Helpers.Config;

/// <summary>Reads an enum only from one of its names, like <see cref="StrictEnumJsonConverter{TEnum}"/> does for JSON.</summary>
public sealed class StrictEnumYamlConverter<TEnum> : IYamlTypeConverter where TEnum : struct, Enum
{
    public bool Accepts(Type type) => type == typeof(TEnum);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return StrictEnumJsonConverter<TEnum>.TryParseName(scalar.Value, out var value)
            ? value
            : throw new YamlException(scalar.Start, scalar.End,
                $"'{scalar.Value}' is not a valid {typeof(TEnum).Name}. Use one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        => emitter.Emit(new Scalar(value == null ? string.Empty : JsonNamingPolicy.CamelCase.ConvertName(value.ToString()!)));
}

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
                    .WithTypeConverter(new StrictEnumYamlConverter<ShuffleMode>())
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
