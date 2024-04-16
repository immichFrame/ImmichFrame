using ImmichFrame.Models;
using Microsoft.Extensions.Options;
using OpenWeatherMap;
using OpenWeatherMap.Models;
using System.Threading.Tasks;

namespace ImmichFrame.Helpers
{
    public class WeatherHelper
    {
        public static Task<WeatherInfo?> GetWeather()
        {
            var settings = Settings.CurrentSettings;
            OpenWeatherMapOptions options = new OpenWeatherMapOptions
            {
                ApiKey = Settings.CurrentSettings.WeatherApiKey,
                UnitSystem = Settings.CurrentSettings.UnitSystem,
                Language = Settings.CurrentSettings.Language,
            };
            return GetWeather(settings.WeatherLat, settings.WeatherLong, options);
        }
        public static async Task<WeatherInfo?> GetWeather(double latitude, double longitude, OpenWeatherMapOptions Options)
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
