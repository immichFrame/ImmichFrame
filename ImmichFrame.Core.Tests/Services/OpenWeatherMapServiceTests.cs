using System.Net;
using System.Text;
using System.Web;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Services;

[TestFixture]
public class OpenWeatherMapServiceTests
{
    private int? _cityId;
    private WeatherCity? _location;
    private string? _coordinates;
    private string? _apiKey;
    private string _units = null!;
    private string _language = null!;
    private WeatherHandler _handler = null!;
    private OpenWeatherMapService _service = null!;

    [SetUp]
    public void Setup()
    {
        _cityId = null;
        _location = null;
        _coordinates = "52.5200,13.4050";
        _apiKey = "weather-key";
        _units = "metric";
        _language = "de";
        _handler = new WeatherHandler();

        var settings = new Mock<IGeneralSettings>();
        settings.SetupGet(s => s.WeatherLocation).Returns(() => _location);
        settings.SetupGet(s => s.WeatherCityId).Returns(() => _cityId);
        settings.SetupGet(s => s.WeatherLatLong).Returns(() => _coordinates);
        settings.SetupGet(s => s.WeatherApiKey).Returns(() => _apiKey);
        settings.SetupGet(s => s.UnitSystem).Returns(() => _units);
        settings.SetupGet(s => s.Language).Returns(() => _language);

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));
        _service = new OpenWeatherMapService(settings.Object, factory.Object);
    }

    [TearDown]
    public void TearDown() => _handler.Dispose();

    [Test]
    public async Task SelectedLocation_UsesItsCoordinatesInsteadOfAmbiguousProviderId()
    {
        _cityId = 2950159;
        _location = new WeatherCity(2643743, "London", "GB", 51.5085, -0.1257);

        var weather = await _service.GetWeather();
        var query = HttpUtility.ParseQueryString(_handler.Requests.Single().Query);

        Assert.Multiple(() =>
        {
            Assert.That(query["id"], Is.Null);
            Assert.That(query["lat"], Is.EqualTo("51.5085"));
            Assert.That(query["lon"], Is.EqualTo("-0.1257"));
            Assert.That(weather!.Location, Is.EqualTo("London"));
            Assert.That(weather.Temperature, Is.EqualTo(6.5));
        });
    }

    [Test]
    public async Task RenamingSelection_DoesNotMutateCachedCoordinateWeather()
    {
        _location = new WeatherCity(2643743, "London", "GB", 51.5085, -0.1257);
        var first = await _service.GetWeather();
        _location = _location with { Name = "Greater London" };
        var renamed = await _service.GetWeather();
        var coordinateWeather = await _service.GetWeather(51.5085, -0.1257);

        Assert.Multiple(() =>
        {
            Assert.That(first!.Location, Is.EqualTo("London"));
            Assert.That(renamed!.Location, Is.EqualTo("Greater London"));
            Assert.That(coordinateWeather!.Location, Is.EqualTo("Berlin"));
            Assert.That(_handler.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ClearingSelection_RestoresOriginalCoordinates()
    {
        _location = new WeatherCity(2643743, "London", "GB", 51.5085, -0.1257);
        await _service.GetWeather();
        _location = null;
        var weather = await _service.GetWeather();
        Assert.That(weather!.Location, Is.EqualTo("Berlin"));
        Assert.That(HttpUtility.ParseQueryString(_handler.Requests.Last().Query)["lat"], Is.EqualTo("52.5200"));
    }

    [Test]
    public async Task CityId_TakesPrecedenceOverUnusableCoordinates()
    {
        _cityId = 2950159;
        _coordinates = "not coordinates";

        var weather = await _service.GetWeather();

        Assert.That(weather, Is.Not.Null);
        var uri = _handler.Requests.Single();
        var query = HttpUtility.ParseQueryString(uri.Query);
        Assert.Multiple(() =>
        {
            Assert.That(uri.Scheme, Is.EqualTo("https"));
            Assert.That(uri.Host, Is.EqualTo("api.openweathermap.org"));
            Assert.That(uri.AbsolutePath, Is.EqualTo("/data/2.5/weather"));
            Assert.That(query["id"], Is.EqualTo("2950159"));
            Assert.That(query["lat"], Is.Null);
            Assert.That(query["lon"], Is.Null);
            Assert.That(query["units"], Is.EqualTo("metric"));
            Assert.That(query["lang"], Is.EqualTo("de"));
            Assert.That(query["appid"], Is.EqualTo("weather-key"));
            Assert.That(weather!.Location, Is.EqualTo("Berlin"));
            Assert.That(weather.Temperature, Is.EqualTo(6.5));
            Assert.That(weather.Unit, Is.EqualTo("°C"));
            Assert.That(weather.Description, Is.EqualTo("Klarer Himmel"));
            Assert.That(weather.IconId, Is.EqualTo("01d"));
        });
    }

    [Test]
    [SetCulture("de-DE")]
    public async Task WithoutCityId_UsesCoordinatesWithInvariantDecimals()
    {
        _coordinates = "52.5200, 13.4050";

        var weather = await _service.GetWeather();

        Assert.That(weather, Is.Not.Null);
        var query = HttpUtility.ParseQueryString(_handler.Requests.Single().Query);
        Assert.Multiple(() =>
        {
            Assert.That(query["lat"], Is.EqualTo("52.5200"));
            Assert.That(query["lon"], Is.EqualTo("13.4050"));
            Assert.That(query["id"], Is.Null);
        });
    }

    [Test]
    public async Task ImperialUnits_PreserveProviderTemperatureAsFahrenheit()
    {
        _units = "imperial";

        var weather = await _service.GetWeather();

        Assert.Multiple(() =>
        {
            Assert.That(weather!.Temperature, Is.EqualTo(6.5));
            Assert.That(weather.Unit, Is.EqualTo("°F"));
            Assert.That(HttpUtility.ParseQueryString(_handler.Requests.Single().Query)["units"], Is.EqualTo("imperial"));
        });
    }

    [TestCase("city")]
    [TestCase("coordinates")]
    [TestCase("units")]
    [TestCase("language")]
    [TestCase("key")]
    public async Task ChangedWeatherSettings_DoNotReusePreviousResponse(string setting)
    {
        await _service.GetWeather();
        switch (setting)
        {
            case "city": _cityId = 2950159; break;
            case "coordinates": _coordinates = "51.5085,-0.1257"; break;
            case "units": _units = "imperial"; break;
            case "language": _language = "en"; break;
            case "key": _apiKey = "replacement-key"; break;
        }

        var weather = await _service.GetWeather();

        Assert.That(weather, Is.Not.Null);
        Assert.That(_handler.Requests, Has.Count.EqualTo(2));
        Assert.That(_handler.Requests[1], Is.Not.EqualTo(_handler.Requests[0]));
    }

    [Test]
    public async Task ClearingCityId_RestoresCoordinateRequests()
    {
        _cityId = 2950159;
        await _service.GetWeather();
        _cityId = null;

        await _service.GetWeather();

        var query = HttpUtility.ParseQueryString(_handler.Requests.Last().Query);
        Assert.That(query["id"], Is.Null);
        Assert.That(query["lat"], Is.EqualTo("52.5200"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task InvalidCityId_DoesNotFallBackToAnotherLocation(int cityId)
    {
        _cityId = cityId;

        Assert.That(await _service.GetWeather(), Is.Null);
        Assert.That(_handler.Requests, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("52.9")]
    [TestCase("52.9,8,3")]
    [TestCase("NaN,8")]
    [TestCase("91,8")]
    [TestCase("52,181")]
    public async Task InvalidCoordinates_ReturnNoWeather(string? coordinates)
    {
        _coordinates = coordinates;

        Assert.That(await _service.GetWeather(), Is.Null);
        Assert.That(_handler.Requests, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task MissingApiKey_DoesNotSendRequest(string? key)
    {
        _apiKey = key;

        Assert.That(await _service.GetWeather(), Is.Null);
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task ProviderError_ReturnsNoWeather()
    {
        _handler.StatusCode = HttpStatusCode.InternalServerError;

        Assert.That(await _service.GetWeather(), Is.Null);
    }

    private sealed class WeatherHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent("""
                    {
                        "coord": { "lon": 13.4050, "lat": 52.5200 },
                        "weather": [{ "id": 800, "main": "Clear", "description": "Klarer Himmel", "icon": "01d" }],
                        "base": "stations",
                        "main": { "temp": 6.5, "feels_like": 5.2, "temp_min": 6.0, "temp_max": 7.0, "pressure": 1020, "humidity": 80, "sea_level": 1020, "grnd_level": 1018 },
                        "visibility": 10000,
                        "wind": { "speed": 2.1, "deg": 180 },
                        "clouds": { "all": 0 },
                        "dt": 1794672000,
                        "sys": { "type": 2, "id": 2000000, "country": "DE", "sunrise": 1794639000, "sunset": 1794670500 },
                        "timezone": 3600,
                        "id": 2950159,
                        "name": "Berlin",
                        "cod": 200
                    }
                    """, Encoding.UTF8, "application/json")
            });
        }
    }
}
