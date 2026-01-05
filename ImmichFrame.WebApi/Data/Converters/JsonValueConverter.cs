using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ImmichFrame.WebApi.Data.Converters;

/// <summary>
/// Stores complex values as JSON text in a SQLite column. Preserves null vs empty.
/// </summary>
public sealed class JsonValueConverter<TModel> : ValueConverter<TModel?, string?>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public JsonValueConverter()
        : base(
            model => model == null ? null : JsonSerializer.Serialize(model, Options),
            json => string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<TModel>(json, Options))
    {
    }
}


