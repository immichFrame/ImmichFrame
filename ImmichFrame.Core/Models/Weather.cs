using ImmichFrame.Core.Interfaces;
using OpenWeatherMap.Models;

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
        public double TempHigh { get; set; } = 0d;
        public double TempLow { get; set; } = 0d;
        public double Precip { get; set; } = 0d;
        public DayForecast[] DailyForecast { get; set; } = [];
        public HourForecast[] HourlyForecast { get; set; } = [];
        public Currently Currently { get; set; } = new Currently();
    }
}
