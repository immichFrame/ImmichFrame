using System.Globalization;
using System.Text.Json;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;
using OpenWeatherMap;
using OpenWeatherMap.Models;

public class OpenWeatherMapService : IWeatherService
{
    public const string HttpClientName = "OpenWeatherMap";

    private readonly IGeneralSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiCache _weatherCache = new ApiCache(TimeSpan.FromMinutes(5));

    public OpenWeatherMapService(IGeneralSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IWeather?> GetWeather()
    {
        if (_settings.WeatherLocation is { } location)
        {
            // Search IDs can identify a surrounding district rather than the selected town.
            // Use the selected coordinates for the data and keep its chosen display name.
            var weather = await GetWeather(location.Latitude, location.Longitude);
            return weather is null ? null : new Weather
            {
                Location = location.Name,
                Temperature = weather.Temperature,
                Unit = weather.Unit,
                TemperatureUnit = weather.TemperatureUnit,
                Description = weather.Description,
                IconId = weather.IconId
            };
        }

        if (_settings.WeatherCityId is int cityId)
        {
            return cityId > 0
                ? await GetWeather($"id={cityId.ToString(CultureInfo.InvariantCulture)}")
                : null;
        }

        var coordinates = _settings.WeatherLatLong?.Split(',', StringSplitOptions.TrimEntries);
        if (coordinates?.Length != 2
            || !double.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
            || !double.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            return null;
        }

        return await GetWeather(latitude, longitude);
    }

    public Task<IWeather?> GetWeather(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude)
            || latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return Task.FromResult<IWeather?>(null);
        }

        return GetWeather(string.Create(CultureInfo.InvariantCulture, $"lat={latitude:0.0000}&lon={longitude:0.0000}"));
    }

    public async Task<List<WeatherCity>> SearchCities(string query, string? apiKey = null, CancellationToken cancellationToken = default)
    {
        query = query.Trim();
        if (query.Length is < 2 or > 100)
            throw new ArgumentException("Enter a city name between 2 and 100 characters.");

        apiKey ??= _settings.WeatherApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Enter an OpenWeatherMap API key before searching for a city.");

        var url = $"https://api.openweathermap.org/data/2.5/find?q={Uri.EscapeDataString(query)}"
            + $"&type=like&cnt=10&appid={Uri.EscapeDataString(apiKey)}";
        using var client = _httpClientFactory.CreateClient(HttpClientName);
        using var document = JsonDocument.Parse(await client.GetStringAsync(url, cancellationToken));
        return document.RootElement.GetProperty("list").EnumerateArray().Take(10)
            .Select(city => new WeatherCity(
                city.GetProperty("id").GetInt32(),
                city.GetProperty("name").GetString() ?? string.Empty,
                city.GetProperty("sys").GetProperty("country").GetString() ?? string.Empty,
                city.GetProperty("coord").GetProperty("lat").GetDouble(),
                city.GetProperty("coord").GetProperty("lon").GetDouble()))
            .ToList();
    }

    private async Task<IWeather?> GetWeather(string locationQuery)
    {
        var apiKey = _settings.WeatherApiKey;
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var units = _settings.UnitSystem ?? "metric";
        var language = _settings.Language;
        var url = $"https://api.openweathermap.org/data/2.5/weather?{locationQuery}"
            + $"&units={Uri.EscapeDataString(units)}&lang={Uri.EscapeDataString(language)}&appid={Uri.EscapeDataString(apiKey)}";

        try
        {
            // Include every request setting so live edits never reuse a previous location's weather.
            return await _weatherCache.GetOrAddAsync(url, async () =>
            {
                using var client = _httpClientFactory.CreateClient(HttpClientName);
                var json = await client.GetStringAsync(url);
                var serializer = new OpenWeatherMapJsonSerializer(units);
                return serializer.DeserializeObject<WeatherInfo>(json).ToWeather();
            });
        }
        catch
        {
            // Weather is optional; an unavailable provider must not interrupt the slideshow.
            return null;
        }
    }
}
