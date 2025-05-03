using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

public class OpenWeatherMapService : IWeatherService
{
    private readonly IServerSettings _settings;
    private readonly ApiCache<IWeather?> _weatherCache = new(TimeSpan.FromMinutes(5));
    public OpenWeatherMapService(IServerSettings settings)
    {
        _settings = settings;
    }

    public async Task<IWeather?> GetWeather()
    {
        return await _weatherCache.GetOrAddAsync("weather", async () =>
        {
            var weatherLatLong = _settings.WeatherLatLong;

            var weatherLat = !string.IsNullOrWhiteSpace(weatherLatLong) ? float.Parse(weatherLatLong!.Split(',')[0]) : 0f;
            var weatherLong = !string.IsNullOrWhiteSpace(weatherLatLong) ? float.Parse(weatherLatLong!.Split(',')[1]) : 0f;

            var weather = await GetWeather(weatherLat, weatherLong);

            return weather;
        });
    }

    public async Task<IWeather?> GetWeather(double latitude, double longitude)
    {
        OpenWeatherMap.OpenWeatherMapOptions options = new OpenWeatherMap.OpenWeatherMapOptions
        {
            ApiKey = _settings.WeatherApiKey,
            UnitSystem = _settings.UnitSystem,
            Language = _settings.Language,
        };

        try
        {
            OpenWeatherMap.IOpenWeatherMapService openWeatherMapService = new OpenWeatherMap.OpenWeatherMapService(options);
            var weatherInfo = await openWeatherMapService.GetCurrentWeatherAsync(latitude, longitude);

            return weatherInfo.ToWeather();
        }
        catch
        {
            //do nothing and return null
        }

        return null;
    }
}