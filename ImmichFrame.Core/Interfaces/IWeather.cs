using ImmichFrame.Core.Models;
namespace ImmichFrame.Core.Interfaces
{
    public interface IWeather
    {
        public string Location { get; set; }
        public double Temperature { get; set; }
        public string Unit { get; set; }
        public string TemperatureUnit { get; set; }
        public string Description { get; set; }
        public string IconId { get; set; }
        public double TempHigh { get; set; }
        public double TempLow { get; set; }
        public DayForecast[] DailyForecast { get; set; }
        public HourForecast[] HourlyForecast { get; set; }
    }
}
