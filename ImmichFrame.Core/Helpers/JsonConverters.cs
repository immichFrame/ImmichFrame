using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImmichFrame.WebApi.Helpers
{
    public class PolymorphicJsonConverter<TInterface> : JsonConverter<TInterface>
    {
        public override TInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                JsonSerializer.Serialize(writer, value, options);
                return;
            }

            var type = value.GetType();
            var json = JsonSerializer.Serialize(value, type, options);

            using (var jsonDoc = JsonDocument.Parse(json))
            {
                writer.WriteStartObject();

                // Write all properties
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    property.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
        }
    }
}
