using System;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using ImmichFrame.Core.Api;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Tests.Mocks;
using ImmichFrame.Core.Interfaces; // Added this back
using System.Text.Json;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers
{
    [TestFixture]
    public class AssetControllerTests
    {
        private WebApplicationFactory<Program> _factory;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Guid _configuredAlbum = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>().WithServerVersion();

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.UseMockHandler(_mockHttpMessageHandler);

                        // Directly instantiate and register settings objects
                        var generalSettings = new GeneralSettings
                        {
                            ShowWeatherDescription = false, // Assuming this corresponds to ShowWeather
                            WeatherIconUrl = "https://openweathermap.org/img/wn/{IconId}.png",
                            ShowClock = true,
                            ClockFormat = "HH:mm",
                            ClockDateFormat = "eee, MMM d",
                            Language = "en",
                            PhotoDateFormat = "MM/dd/yyyy", // Crucial for the NRE
                            ImageLocationFormat = "City,State,Country",
                            DownloadImages = false,
                            RenewImagesDuration = 30,
                            // Ensure all non-nullable string properties that might be used have defaults if not set here
                            PrimaryColor = "#FFFFFF", // Example default
                            SecondaryColor = "#000000", // Example default
                            Style = "none",
                            BaseFontSize = "16px",
                            WeatherApiKey = "",
                            UnitSystem = "imperial",
                            WeatherLatLong = "0,0"
                        };

                        var accountSettings = new ServerAccountSettings
                        {
                            Name = "family",
                            ImmichServerUrl = "http://mock-immich-server.com",
                            ApiKey = "test-api-key",
                            ShowMemories = false,
                            ShowFavorites = true,
                            ShowArchived = false,
                            Albums = new List<Guid> { _configuredAlbum },
                            ExcludedAlbums = new List<Guid>(),
                            People = new List<Guid>()
                        };

                        var serverSettings = new ServerSettings
                        {
                            GeneralSettingsImpl = generalSettings,
                            AccountsImpl = new List<ServerAccountSettings> { accountSettings }
                        };

                        services.AddSingleton<ISettingsProvider>(new StaticSettingsProvider(serverSettings));
                        services.AddSingleton<IServerSettings>(serverSettings);
                        services.AddSingleton<IGeneralSettings>(generalSettings);
                        // Ensure IAccountSettings can be resolved if needed by MultiImmichFrameLogicDelegate directly
                        // However, PooledImmichFrameLogic receives IAccountSettings via the factory Func
                    });
                });
        }

        // Removed OneTimeSetup that created Settings.json

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
        }

        [Test]
        public async Task GetRandomImage_ReturnsImageFromMockServer()
        {
            // Arrange
            var expectedAssetId = Guid.NewGuid();

            // Account selection asks for the match count before the random pick.
            var statsJson = JsonSerializer.Serialize(new SearchStatisticsResponseDto { Total = 1 });
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/search/statistics")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(statsJson)
                });

            // Build the mock response from the generated DTOs so the test fails at
            // compile time (not at runtime) if the OpenAPI schema changes.
            var randomJson = JsonSerializer.Serialize(new[] { BuildAssetResponse(expectedAssetId) });
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/search/random")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(randomJson)
                });

            // Setup for ViewAssetAsync (thumbnail)
            var mockImageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // Minimal JPEG
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/thumbnail")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(mockImageData)
                });

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Asset/RandomImageAndInfo");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            // For now, we'll just check if the response is not empty.
            // A more robust check would be to deserialize the response and check the asset ID.
            Assert.That(content, Is.Not.Empty);
        }

        // Answers /search/statistics and /search/random, recording each /search/random request body.
        private List<string> CaptureRandomSearches()
        {
            var bodies = new List<string>();
            var randomJson = JsonSerializer.Serialize(new[] { BuildAssetResponse(Guid.NewGuid()) });
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/search/random")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => bodies.Add(req.Content!.ReadAsStringAsync().Result))
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(randomJson)
                });
            var statsJson = JsonSerializer.Serialize(new SearchStatisticsResponseDto { Total = 1 });
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/search/statistics")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(statsJson)
                });
            return bodies;
        }

        [Test]
        public async Task GetAssets_AccountWithAlbums_SearchesOnlyThoseAlbums()
        {
            var bodies = CaptureRandomSearches();
            var album1 = Guid.NewGuid();
            var album2 = Guid.NewGuid();
            var client = _factory.CreateClient();

            var response = await client.GetAsync($"/api/Asset?accountName=Family&albumIds={album1}, {album2}");

            response.EnsureSuccessStatusCode();
            Assert.That(bodies, Has.Count.EqualTo(1));
            Assert.That(bodies[0], Does.Contain(album1.ToString()).And.Contain(album2.ToString()));
            Assert.That(bodies[0], Does.Not.Contain(_configuredAlbum.ToString()));
        }

        [Test]
        public async Task GetAssets_AlbumsWithoutAccount_AreIgnored()
        {
            var bodies = CaptureRandomSearches();
            var album = Guid.NewGuid();
            var client = _factory.CreateClient();

            var response = await client.GetAsync($"/api/Asset?albumIds={album}");

            response.EnsureSuccessStatusCode();
            Assert.That(bodies, Is.Not.Empty);
            Assert.That(bodies.Any(b => b.Contains(album.ToString())), Is.False);
        }

        [Test]
        public async Task GetAssets_InvalidAlbums_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/Asset?accountName=family&albumIds=not-an-id");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task GetAssets_UnknownAccount_ReturnsEmptyList()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/Asset?accountName=unknown");

            response.EnsureSuccessStatusCode();
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("[]"));
        }

        // Builds a valid AssetResponseDto from the generated client types. Only the
        // fields required by the schema are set; pass type/duration to reuse this for
        // video assets. Changes to the OpenAPI schema surface here as compile errors.
        private static AssetResponseDto BuildAssetResponse(
            Guid id,
            AssetTypeEnum type = AssetTypeEnum.IMAGE,
            int? duration = null)
        {
            var timestamp = DateTimeOffset.Parse("2023-10-26T10:00:00Z");
            return new AssetResponseDto
            {
                Id = id,
                OwnerId = Guid.NewGuid(),
                Type = type,
                OriginalPath = "/path/to/image.jpg",
                OriginalFileName = "image.jpg",
                Checksum = "testchecksum",
                Thumbhash = "I0cMCQS94XmImZeXmYd3d3g=",
                Visibility = AssetVisibility.Timeline,
                Duration = duration,
                Width = 1920,
                Height = 1080,
                IsFavorite = true,
                HasMetadata = true,
                FileCreatedAt = timestamp,
                FileModifiedAt = timestamp,
                LocalDateTime = timestamp,
                CreatedAt = timestamp,
                UpdatedAt = timestamp,
            };
        }

        // TODO: Fix Test
        // [Test]
        // public async Task GetImage_VideoAsset_ReturnsVideoStream()
        // {
        //     // Arrange
        //     var videoAssetId = Guid.NewGuid();
        //     var assetInfoJson = $@"
        //     {{
        //         ""id"": ""{videoAssetId}"",
        //         ""originalFileName"": ""test-video.mp4"",
        //         ""type"": ""VIDEO"",
        //         ""fileCreatedAt"": ""2023-10-26T10:00:00Z"",
        //         ""fileModifiedAt"": ""2023-10-26T10:00:00Z"",
        //         ""duration"": ""0:00:05"",
        //         ""checksum"": ""checksum"",
        //         ""deviceAssetId"": ""deviceAsset"",
        //         ""deviceId"": ""device"",
        //         ""ownerId"": ""owner"",
        //         ""localDateTime"": ""2023-10-26T10:00:00Z"",
        //         ""visibility"": ""timeline"",
        //         ""hasMetadata"": true,
        //         ""isArchived"": false,
        //         ""isOffline"": false,
        //         ""isTrashed"": false,
        //         ""updatedAt"": ""2023-10-26T10:00:00Z""
        //     }}";

        //     _mockHttpMessageHandler.Protected()
        //         .Setup<Task<HttpResponseMessage>>(
        //             "SendAsync",
        //             ItExpr.Is<HttpRequestMessage>(req =>
        //                 req.Method == HttpMethod.Get &&
        //                 req.RequestUri!.ToString().EndsWith($"/assets/{videoAssetId}", StringComparison.OrdinalIgnoreCase)),
        //             ItExpr.IsAny<CancellationToken>()
        //         )
        //         .ReturnsAsync(() => new HttpResponseMessage
        //         {
        //             StatusCode = HttpStatusCode.OK,
        //             Content = new StringContent(assetInfoJson, Encoding.UTF8, "application/json")
        //         });

        //     var videoBytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
        //     _mockHttpMessageHandler.Protected()
        //         .Setup<Task<HttpResponseMessage>>(
        //             "SendAsync",
        //             ItExpr.Is<HttpRequestMessage>(req =>
        //                 req.Method == HttpMethod.Get &&
        //                 req.RequestUri!.ToString().Contains($"/assets/{videoAssetId}/video/playback", StringComparison.OrdinalIgnoreCase)),
        //             ItExpr.IsAny<CancellationToken>()
        //         )
        //         .ReturnsAsync(() =>
        //         {
        //             var response = new HttpResponseMessage
        //             {
        //                 StatusCode = HttpStatusCode.OK,
        //                 Content = new ByteArrayContent(videoBytes)
        //             };
        //             response.Content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        //             return response;
        //         });

        //     var client = _factory.CreateClient();

        //     // Act
        //     var response = await client.GetAsync($"/api/Asset/{videoAssetId}/Asset?assetType=1");

        //     // Assert
        //     response.EnsureSuccessStatusCode();
        //     Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("video/mp4"));
        //     var resultBytes = await response.Content.ReadAsByteArrayAsync();
        //     Assert.That(resultBytes, Is.EqualTo(videoBytes));
        // }
    }
}
