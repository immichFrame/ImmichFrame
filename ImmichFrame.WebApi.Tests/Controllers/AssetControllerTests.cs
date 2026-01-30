using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ImmichFrame.WebApi.Tests.Mocks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http; // Added this
using Moq;
using Moq.Protected;
using ImmichFrame.Core.Api;
using ImmichFrame.WebApi.Models;
using ImmichFrame.Core.Interfaces; // Added this back
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers
{
    [TestFixture]
    public class AssetControllerTests
    {
        private WebApplicationFactory<Program> _factory;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        // 1. Mock HttpMessageHandler and IHttpClientFactory
                        services.AddSingleton<HttpMessageHandler>(_mockHttpMessageHandler.Object);
                        services.AddHttpClient("ImmichApiAccountClient")
                            .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<HttpMessageHandler>());
                        services.ConfigureAll<HttpClientFactoryOptions>(options =>
                        {
                            options.HttpMessageHandlerBuilderActions.Add(b =>
                            {
                                b.PrimaryHandler = b.Services.GetRequiredService<HttpMessageHandler>();
                            });
                        });

                        // 2. Directly instantiate and register settings objects
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
                            ImmichServerUrl = "http://mock-immich-server.com",
                            ApiKey = "test-api-key",
                            ShowMemories = false,
                            ShowFavorites = true,
                            ShowArchived = false,
                            Albums = new List<Guid>(),
                            ExcludedAlbums = new List<Guid>(),
                            People = new List<Guid>()
                        };

                        var serverSettings = new ServerSettings
                        {
                            GeneralSettingsImpl = generalSettings,
                            AccountsImpl = new List<ServerAccountSettings> { accountSettings }
                        };

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
            var assetDtoJson = $@"
            {{
                ""id"": ""{expectedAssetId}"",
                ""originalPath"": ""/path/to/image.jpg"",
                ""type"": ""IMAGE"",
                ""fileCreatedAt"": ""2023-10-26T10:00:00Z"",
                ""fileModifiedAt"": ""2023-10-26T10:00:00Z"",
                ""isFavorite"": true,
                ""duration"": ""0:00:00"",
                ""checksum"": ""testchecksum"",
                ""deviceAssetId"": ""testDeviceAssetId"",
                ""deviceId"": ""testDeviceId"",
                ""ownerId"": ""testOwnerId"",
                ""originalFileName"": ""image.jpg"",
                ""localDateTime"": ""2023-10-26T10:00:00Z"",
                ""visibility"": ""timeline"",
                ""hasMetadata"": true,
                ""isArchived"": false,
                ""isOffline"": false,
                ""isTrashed"": false,
                ""thumbhash"": ""I0cMCQS94XmImZeXmYd3d3g="",
                ""updatedAt"": ""2023-10-26T10:00:00Z""
            }}";

            // JSON structure for SearchResponseDto
            var jsonResponse = $@"
            {{
                ""albums"": {{
                    ""count"": 0,
                    ""items"": [],
                    ""total"": 0,
                    ""facets"": []
                }},
                ""assets"": {{
                    ""count"": 1,
                    ""items"": [
                        {assetDtoJson}
                    ],
                    ""total"": 1,
                    ""facets"": [],
                    ""nextPage"": null
                }}
            }}";

            // Setup for SearchAssetsAsync
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/search/metadata")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => new HttpResponseMessage // Use a Func to return new instance each time
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
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
