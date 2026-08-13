using OpenWeatherMap.Models;
using ImmichFrame.Core.Models;
using UnitsNet;
using UnitsNet.Units;

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
                MinimumTemperature = GetOptionalTemperatureValue(weatherInfo.Main.MinimumTemperature),
                MaximumTemperature = GetOptionalTemperatureValue(weatherInfo.Main.MaximumTemperature),
                Humidity = weatherInfo.Main.Humidity.Value,
                Unit = Temperature.GetAbbreviation(weatherInfo.Main.Temperature.Unit),
                TemperatureUnit = weatherInfo.Main.Temperature.ToString(),
                IconId = $"{string.Join(',', weatherInfo.Weather.Select(x => x.IconId))}"
            };
        }

        private static double? GetOptionalTemperatureValue(Temperature temperature)
        {
            return temperature.Value == 0d && temperature.Unit == TemperatureUnit.Kelvin ? null : temperature.Value;
        }
    }
}
