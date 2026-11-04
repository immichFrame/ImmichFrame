using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImmichFrame.Core.Models;

/// <summary>How an account's assets are ordered.</summary>
[JsonConverter(typeof(StrictEnumJsonConverter<ShuffleMode>))]
public enum ShuffleMode
{
    /// <summary>Each batch is an independent random pick, so assets can repeat.</summary>
    Random = 0,

    /// <summary>Every matching asset is shown once, in random order, before any repeats; each client keeps its own order.</summary>
    Exhaustive = 1
}

/// <summary>
/// Reads an enum only from one of its names (any case) and writes it in camelCase. Numbers, numeric
/// strings and names the enum doesn't define are refused, so a typo can never silently pick a mode.
/// </summary>
public sealed class StrictEnumJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"{typeof(TEnum).Name} must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }

        return TryParseName(reader.GetString(), out var value)
            ? value
            : throw new JsonException($"'{reader.GetString()}' is not a valid {typeof(TEnum).Name}. Use one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => writer.WriteStringValue(JsonNamingPolicy.CamelCase.ConvertName(value.ToString()));

    /// <summary>Parses a defined name of the enum, ignoring case. Numbers are not names.</summary>
    public static bool TryParseName(string? text, out TEnum value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text) || !text.All(char.IsAsciiLetter))
        {
            return false;
        }

        return Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value);
    }
}
