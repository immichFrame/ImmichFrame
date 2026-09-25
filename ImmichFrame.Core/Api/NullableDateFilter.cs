using Newtonsoft.Json;

namespace ImmichFrame.Core.Api
{
    /// <summary>
    /// <see cref="DateFilterNullable"/> whose <c>eq</c>/<c>ne</c> can be set to JSON null.
    /// The generated properties cannot tell "unset" from "explicitly null", and NSwag marks
    /// them <c>NullValueHandling.Ignore</c>, so <c>Eq = null</c> on the generated type is omitted.
    /// </summary>
    [JsonConverter(typeof(NullableDateFilterConverter))]
    public sealed class NullableDateFilter : DateFilterNullable
    {
        [JsonIgnore]
        public new FilterValue<DateTimeOffset?> Eq { get; set; }

        [JsonIgnore]
        public new FilterValue<DateTimeOffset?> Ne { get; set; }
    }

    /// <summary>
    /// A filter operand that remembers whether it was assigned, including an assignment of null.
    /// </summary>
    public readonly struct FilterValue<T>
    {
        public bool IsSpecified { get; }
        public T Value { get; }

        public FilterValue(T value)
        {
            IsSpecified = true;
            Value = value;
        }

        public static implicit operator FilterValue<T>(T value) => new(value);
    }

    internal sealed class NullableDateFilterConverter : JsonConverter<NullableDateFilter>
    {
        public override bool CanRead => false;

        public override NullableDateFilter ReadJson(JsonReader reader, Type objectType, NullableDateFilter? existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotSupportedException();

        public override void WriteJson(JsonWriter writer, NullableDateFilter? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            WriteSpecified(writer, serializer, "eq", value.Eq);
            WriteSpecified(writer, serializer, "ne", value.Ne);
            WriteDate(writer, serializer, "gt", value.Gt);
            WriteDate(writer, serializer, "gte", value.Gte);
            WriteDate(writer, serializer, "lt", value.Lt);
            WriteDate(writer, serializer, "lte", value.Lte);
            writer.WriteEndObject();
        }

        private static void WriteSpecified(JsonWriter writer, JsonSerializer serializer, string name, FilterValue<DateTimeOffset?> value)
        {
            if (!value.IsSpecified)
                return;

            writer.WritePropertyName(name);
            serializer.Serialize(writer, value.Value);
        }

        private static void WriteDate(JsonWriter writer, JsonSerializer serializer, string name, DateTimeOffset? value)
        {
            if (value == null)
                return;

            writer.WritePropertyName(name);
            serializer.Serialize(writer, value);
        }
    }
}
