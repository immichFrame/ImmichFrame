using OpenWeatherMap.Models;
using ImmichFrame.Core.Models;
using UnitsNet;

namespace ImmichFrame.Core.Helpers
{
    public static class WeatherExtensions
    {
        public static Weather ToWeather(this WeatherInfo? weatherInfo)
        {
            if (weatherInfo == null) return new Weather();

            return new Weather
            {
                Location = weatherInfo.CityName,
                Description = $"{string.Join(',', weatherInfo.Weather.Select(x => x.Description))}",
                Temperature = weatherInfo.Main.Temperature.Value,
                Unit = Temperature.GetAbbreviation(weatherInfo.Main.Temperature.Unit),
                TemperatureUnit = weatherInfo.Main.Temperature.ToString(),
                IconId = $"{string.Join(',', weatherInfo.Weather.Select(x => x.IconId))}"
            };
        }
    }
}
