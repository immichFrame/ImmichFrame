using ImmichFrame.Models;
using OpenWeatherMap;
using OpenWeatherMap.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImmichFrame.Helpers
{
    public class WeatherHelper
    {
        private static readonly OpenWeatherMapOptions Options = new OpenWeatherMapOptions
        {
            ApiKey = Settings.CurrentSettings.WeatherApiKey,
            UnitSystem = Settings.CurrentSettings.UnitSystem,
            Language = Settings.CurrentSettings.Language,
        };
        public static Task<WeatherInfo?> GetWeather()
        {
            var settings = Settings.CurrentSettings;
            return GetWeather(settings.WeatherLat, settings.WeatherLong);
        }
        public static async Task<WeatherInfo?> GetWeather(double latitude, double longitude)
        {
            try
            {
                IOpenWeatherMapService openWeatherMapService = new OpenWeatherMapService(Options);
                var weatherInfo = await openWeatherMapService.GetCurrentWeatherAsync(latitude, longitude);

                return weatherInfo;
            }
            catch
            {
                //do nothing and return null
            }

            return null;
        }
    }
}
