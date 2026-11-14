using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Web;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Tests.Mocks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers;

[TestFixture]
[NonParallelizable]
public class WeatherAdminControllerTests
{
    private const string AdminPassword = "weather-admin-password";
    private string _configDir = null!;
    private CitySearchHandler _handler = null!;
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void Setup()
    {
        _configDir = Path.Combine(Path.GetTempPath(), "immichframe-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_configDir);
        Environment.SetEnvironmentVariable("IMMICHFRAME_CONFIG_PATH", _configDir);
        Environment.SetEnvironmentVariable("IMMICHFRAME_ADMIN_PASSWORD", AdminPassword);
        _handler = new CitySearchHandler();
        var settings = new Mock<IGeneralSettings>();
        settings.SetupGet(s => s.WeatherApiKey).Returns("saved-weather-key");
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));
        var weatherService = new OpenWeatherMapService(settings.Object, factory.Object);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.UseMockHandler(new Mock<HttpMessageHandler>().WithServerVersion());
                services.AddSingleton<IWeatherService>(weatherService);
            }));
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
        _handler.Dispose();
        Environment.SetEnvironmentVariable("IMMICHFRAME_CONFIG_PATH", null);
        Environment.SetEnvironmentVariable("IMMICHFRAME_ADMIN_PASSWORD", null);
        Directory.Delete(_configDir, true);
    }

    private HttpClient CreateClient(bool authenticated = true)
    {
        var client = _factory.CreateClient();
        if (authenticated)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminPassword);
        return client;
    }

    [Test]
    public async Task SearchCities_RequiresAdminAuthentication()
    {
        using var client = CreateClient(authenticated: false);

        var response = await client.PostAsJsonAsync("/api/Admin/Weather/SearchCities", new { query = "London" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task SearchCities_ReturnsCityDetailsAndUsesUnsavedApiKey()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Admin/Weather/SearchCities",
            new { query = " London ", apiKey = "edited-weather-key" });

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var cities = JsonNode.Parse(json)!.AsArray();
        var request = _handler.Requests.Single();
        var query = HttpUtility.ParseQueryString(request.Query);
        Assert.Multiple(() =>
        {
            Assert.That(cities, Has.Count.EqualTo(2));
            Assert.That((int)cities[0]!["id"]!, Is.EqualTo(2643743));
            Assert.That((int)cities[1]!["id"]!, Is.EqualTo(6058560));
            Assert.That((string)cities[0]!["name"]!, Is.EqualTo("London"));
            Assert.That((string)cities[0]!["country"]!, Is.EqualTo("GB"));
            Assert.That((double)cities[0]!["latitude"]!, Is.EqualTo(51.5085));
            Assert.That((double)cities[1]!["longitude"]!, Is.EqualTo(-81.233));
            Assert.That(query["q"], Is.EqualTo("London"));
            Assert.That(query["appid"], Is.EqualTo("edited-weather-key"));
            Assert.That(query["cnt"], Is.EqualTo("10"));
            Assert.That(request.Scheme, Is.EqualTo("https"));
            Assert.That(json, Does.Not.Contain("edited-weather-key"));
            Assert.That(json, Does.Not.Contain("saved-weather-key"));
        });
    }

    [Test]
    public async Task SearchCities_UsesSavedApiKeyWhenNoDraftKeyIsProvided()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Admin/Weather/SearchCities", new { query = "London" });

        response.EnsureSuccessStatusCode();
        Assert.That(HttpUtility.ParseQueryString(_handler.Requests.Single().Query)["appid"], Is.EqualTo("saved-weather-key"));
    }

    [TestCase("")]
    [TestCase("a")]
    [TestCase("  ")]
    public async Task SearchCities_RejectsShortQueriesWithoutContactingProvider(string query)
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Admin/Weather/SearchCities", new { query });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task SearchCities_RejectsOverlongQueriesWithoutContactingProvider()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Admin/Weather/SearchCities", new { query = new string('a', 101) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task SearchCities_RejectsEmptyDraftKeyInsteadOfUsingSavedKey()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/Admin/Weather/SearchCities", new { query = "London", apiKey = "" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task SearchCities_ProviderErrorReturns502WithoutExposingKey()
    {
        using var client = CreateClient();
        _handler.StatusCode = HttpStatusCode.Unauthorized;

        var response = await client.PostAsJsonAsync("/api/Admin/Weather/SearchCities",
            new { query = "London", apiKey = "edited-weather-key" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("edited-weather-key"));
    }

    [Test]
    public async Task SearchCities_NoMatchesReturnsEmptyList()
    {
        using var client = CreateClient();
        _handler.Body = """{"cod":"200","count":0,"list":[]}""";

        var response = await client.PostAsJsonAsync("/api/Admin/Weather/SearchCities", new { query = "MissingCity" });

        response.EnsureSuccessStatusCode();
        Assert.That(JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray(), Is.Empty);
    }

    private sealed class CitySearchHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string Body { get; set; } = """
            {"cod":"200","count":2,"list":[
                {"id":2643743,"name":"London","coord":{"lat":51.5085,"lon":-0.1257},"sys":{"country":"GB"}},
                {"id":6058560,"name":"London","coord":{"lat":42.9834,"lon":-81.233},"sys":{"country":"CA"}}
            ]}
            """;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent(Body) });
        }
    }
}
