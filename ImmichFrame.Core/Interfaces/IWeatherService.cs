using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;

public interface IWeatherService
{
    Task<IWeather?> GetWeather();
    Task<IWeather?> GetWeather(double latitude, double longitude);
    Task<List<WeatherCity>> SearchCities(string query, string? apiKey = null, CancellationToken cancellationToken = default);
}
