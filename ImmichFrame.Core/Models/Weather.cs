using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Models
{
    public class Weather : IWeather
    {
        public string Location { get; set; } = "";
        public double Temperature { get; set; } = 0d;
        public string Unit { get; set; } = "";
        public string TemperatureUnit { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconId { get; set; } = "";
    }
}
