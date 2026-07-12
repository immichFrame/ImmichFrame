using System.Text.Json;
using System.Text.Json.Nodes;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers
{
    [TestFixture]
    public class ConfigControllerTests
    {
        private WebApplicationFactory<Program> _factory;
        private GeneralSettings _generalSettings;

        [SetUp]
        public void Setup()
        {
            var generalSettings = new GeneralSettings
            {
                Interval = 33,
                TransitionDuration = 2.5,
                DownloadImages = true,
                RenewImagesDuration = 77,
                ShowClock = false,
                ClockFormat = "HH:mm:ss",
                ClockDateFormat = "yyyy-MM-dd",
                ShowPhotoDate = false,
                ShowProgressBar = false,
                PhotoDateFormat = "dd.MM.yyyy",
                ShowImageDesc = false,
                ShowPeopleDesc = false,
                ShowTagsDesc = false,
                ShowAlbumName = false,
                ShowImageLocation = false,
                ImageLocationFormat = "City",
                PrimaryColor = "#112233",
                SecondaryColor = "#445566",
                Style = "blur",
                BaseFontSize = "18px",
                ShowWeatherDescription = false,
                WeatherIconUrl = "https://example.com/{IconId}.png",
                ImageZoom = false,
                ImagePan = true,
                ImageFill = true,
                PlayAudio = true,
                Layout = "single",
                Language = "de",
                // Server-only values that must never surface in the client config:
                WeatherApiKey = "secret-weather-key",
                WeatherLatLong = "1.23,4.56",
                UnitSystem = "metric",
                Webhook = "https://webhook.example.com/secret-hook",
                AuthenticationSecret = "secret-auth-token",
                Webcalendars = new List<string> { "https://calendar.example.com/secret.ics" },
                RefreshAlbumPeopleInterval = 8,
            };
            _generalSettings = generalSettings;

            var serverSettings = new ServerSettings
            {
                GeneralSettingsImpl = generalSettings,
                AccountsImpl = new List<ServerAccountSettings>
                {
                    new()
                    {
                        ImmichServerUrl = "http://mock-immich-server.com",
                        ApiKey = "secret-api-key",
                    }
                }
            };

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddSingleton<IServerSettings>(serverSettings);
                        services.AddSingleton<IGeneralSettings>(generalSettings);
                    });
                });
        }

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
        }

        [Test]
        public async Task GetConfig_ReturnsSerializedClientSettings()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/Config");

            response.EnsureSuccessStatusCode();
            var actual = JsonNode.Parse(await response.Content.ReadAsStringAsync());

            var expected = JsonSerializer.SerializeToNode(
                new ClientSettingsDto(_generalSettings),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.That(JsonNode.DeepEquals(actual, expected), Is.True,
                () => $"Expected: {expected?.ToJsonString()}\nActual: {actual?.ToJsonString()}");
        }

        [Test]
        public async Task GetConfig_ContainsNoSecrets()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/Config");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Not.Contain("secret-auth-token"));
                Assert.That(json, Does.Not.Contain("secret-weather-key"));
                Assert.That(json, Does.Not.Contain("secret-api-key"));
                Assert.That(json, Does.Not.Contain("secret-hook"));
                Assert.That(json, Does.Not.Contain("secret.ics"));
                Assert.That(json, Does.Not.Contain("authenticationSecret"));
                Assert.That(json, Does.Not.Contain("weatherApiKey"));
                Assert.That(json, Does.Not.Contain("apiKey"));
                Assert.That(json, Does.Not.Contain("webhook"));
                Assert.That(json, Does.Not.Contain("webcalendars"));
            });
        }
    }
}
