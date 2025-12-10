using System.Text.Json.Serialization;

namespace ImmichFrame.Core.Models
{
    public class PirateWeatherResponse
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("currently")]
        public Currently? Currently { get; set; }

        [JsonPropertyName("daily")]
        public Daily? Daily { get; set; }

        [JsonPropertyName("hourly")]
        public Hourly? Hourly { get; set; }

        [JsonPropertyName("minutely")]
        public Minutely? Minutely { get; set; }

    }

    public class Currently
    {
        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("precipProbability")]
        public double PrecipProbability { get; set; }

        [JsonPropertyName("apparentTemperature")]
        public double ApparentTemperature { get; set; }
    }

    public class Daily
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("data")]
        public List<DayForecast>? Data { get; set; }
    }

    public class DayForecast
    {
        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("temperatureMax")]
        public double TemperatureMax { get; set; }

        [JsonPropertyName("temperatureMin")]
        public double TemperatureMin { get; set; }

        [JsonPropertyName("precipProbability")]
        public double PrecipProbability { get; set; }
    }

    public class Hourly
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("data")]
        public List<HourForecast>? Data { get; set; }
    }

    public class HourForecast
    {
        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("precipProbability")]
        public double PrecipProbability { get; set; }
    }

    public class Minutely
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

    }
}