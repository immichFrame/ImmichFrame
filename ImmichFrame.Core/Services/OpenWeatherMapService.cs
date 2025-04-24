using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;

public class OpenWeatherMapService : IWeatherService
{
    private readonly IServerSettings _settings;
    public OpenWeatherMapService(IServerSettings settings)
    {
        _settings = settings;
    }

    private (DateTime fetchDate, IWeather? weather)? lastWeather;
    public async Task<IWeather?> GetWeather()
    {
        // Check if cached weather data is still valid
        if (lastWeather.HasValue && lastWeather.Value.weather != null && lastWeather.Value.fetchDate.AddMinutes(5) > DateTime.Now)
        {
            return lastWeather.Value.weather;
        }

        var weatherLatLong = _settings.WeatherLatLong;

        var weatherLat = !string.IsNullOrWhiteSpace(weatherLatLong) ? float.Parse(weatherLatLong!.Split(',')[0]) : 0f;
        var weatherLong = !string.IsNullOrWhiteSpace(weatherLatLong) ? float.Parse(weatherLatLong!.Split(',')[1]) : 0f;

        var weather = await GetWeather(weatherLat, weatherLong);

        lastWeather = (DateTime.Now, weather);

        return weather;
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