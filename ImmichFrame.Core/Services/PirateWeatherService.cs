using System.Text.Json;
using ImmichFrame.Core.Models;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Helpers;


namespace ImmichFrame.Core.Services
{
    public class PirateWeatherService : IWeatherService
    {
        private readonly IGeneralSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly IApiCache _weatherCache = new ApiCache(TimeSpan.FromMinutes(5));

        public PirateWeatherService(IGeneralSettings settings, HttpClient httpClient)
        {
            _settings = settings;
            _httpClient = httpClient;
        }

        public async Task<IWeather?> GetWeather()
        {
            return await _weatherCache.GetOrAddAsync("weather", async () =>
            {
                var weatherLatLong = _settings.WeatherLatLong;
                var weatherLat = !string.IsNullOrWhiteSpace(weatherLatLong) ? float.Parse(weatherLatLong!.Split(',')[0]) : 0f;
                var weatherLong = !string.IsNullOrWhiteSpace(weatherLatLong) ? float.Parse(weatherLatLong!.Split(',')[1]) : 0f;

                return await GetWeather(weatherLat, weatherLong);
            });
        }

        public async Task<IWeather?> GetWeather(double latitude, double longitude)
        {
            try
            {
                var units = _settings.UnitSystem?.ToLower() == "metric" ? "si" : "us";
                var url = $"https://api.pirateweather.net/forecast/{_settings.WeatherApiKey}/{latitude},{longitude}?units={units}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<PirateWeatherResponse>(json);

                // Create Summmary Field
                var summary = data?.Minutely?.Summary + " " + data?.Hourly?.Summary + " " + data?.Daily?.Summary;

                return new Weather
                {
                    IconId = data?.Currently?.Icon ?? "",
                    Temperature = data?.Currently?.Temperature ?? 0,
                    TempHigh = data?.Daily?.Data?[0].TemperatureHigh ?? 0,
                    TempLow = data?.Daily?.Data?[0].TemperatureLow ?? 0,
                    Precip = data?.Currently?.PrecipProbability ?? 0,
                    Unit = units == "si" ? "°C" : "°F",
                    Description = summary ?? "",
                    HourlyForecast = data?.Hourly?.Data?.Skip(1).Where((_, index) => index % 2 == 0).Take(6).ToArray() ?? [],
                    DailyForecast = data?.Daily?.Data?.Skip(1).Take(3).ToArray() ?? [],
                };
            }
            catch
            {
                return null;
            }
        }
    }
}