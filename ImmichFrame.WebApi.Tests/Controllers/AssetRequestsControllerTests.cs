using System.Net.Http.Json;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using ImmichFrame.Core.Interfaces;
using System.Net;

namespace ImmichFrame.WebApi.Tests.Controllers;

[TestFixture]
public class AssetRequestsControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
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

                    var generalSettings = new GeneralSettings
                    {
                        ShowWeatherDescription = false,
                        WeatherIconUrl = "https://openweathermap.org/img/wn/{IconId}.png",
                        ShowClock = true,
                        ClockFormat = "HH:mm",
                        ClockDateFormat = "eee, MMM d",
                        Language = "en",
                        PhotoDateFormat = "MM/dd/yyyy",
                        ImageLocationFormat = "City,State,Country",
                        DownloadImages = false,
                        RenewImagesDuration = 30,
                        PrimaryColor = "#FFFFFF",
                        SecondaryColor = "#000000",
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
                        Albums = [],
                        ExcludedAlbums = [],
                        People = []
                    };

                    var serverSettings = new ServerSettings
                    {
                        GeneralSettingsImpl = generalSettings,
                        AccountsImpl = [accountSettings]
                    };

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
    public async Task GetRecentAssetRequests_ReturnsLocationAndTakenTime()
    {
        var assetId = Guid.NewGuid();
        SetupRecentAssetRequestMocks(assetId);

        var client = _factory.CreateClient();
        var imageResponse = await client.GetAsync("/api/Asset/RandomImageAndInfo?clientIdentifier=KitchenPanel");
        imageResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/AssetRequests/RecentAssetRequests?limit=10");
        response.EnsureSuccessStatusCode();

        var recentRequests = await response.Content.ReadFromJsonAsync<List<RecentAssetRequestDetailsRecord>>();

        Assert.That(recentRequests, Is.Not.Null);
        var recentRequest = recentRequests!.Single(item => item.AssetId == assetId);
        Assert.That(recentRequest.CameraDisplay, Is.EqualTo("Canon EOS R6"));
        Assert.That(recentRequest.Location, Is.EqualTo("Paris, Ile-de-France, France"));
        Assert.That(recentRequest.TakenAtUtc, Is.Not.Null);
    }

    private void SetupRecentAssetRequestMocks(Guid assetId)
    {
        const string assetInfoTemplate = """
        {
            "id": "%ASSET_ID%",
            "originalPath": "/path/to/image.jpg",
            "type": "IMAGE",
            "fileCreatedAt": "2023-10-26T10:00:00Z",
            "fileModifiedAt": "2023-10-26T10:00:00Z",
            "isFavorite": true,
            "duration": "0:00:00",
            "checksum": "testchecksum",
            "deviceAssetId": "testDeviceAssetId",
            "deviceId": "testDeviceId",
            "ownerId": "testOwnerId",
            "originalFileName": "kitchen-photo.jpg",
            "localDateTime": "2023-10-26T10:00:00Z",
            "visibility": "timeline",
            "hasMetadata": true,
            "isArchived": false,
            "isOffline": false,
            "isTrashed": false,
            "thumbhash": "I0cMCQS94XmImZeXmYd3d3g=",
            "updatedAt": "2023-10-26T10:00:00Z",
            "exifInfo": {
                "make": "Canon",
                "model": "EOS R6",
                "city": "Paris",
                "state": "Ile-de-France",
                "country": "France"
            }
        }
        """;

        var assetInfoJson = assetInfoTemplate.Replace("%ASSET_ID%", assetId.ToString());
        var searchResponseJson = $$"""
        {
            "albums": {
                "count": 0,
                "items": [],
                "total": 0,
                "facets": []
            },
            "assets": {
                "count": 1,
                "items": [
                    {{assetInfoJson}}
                ],
                "total": 1,
                "facets": [],
                "nextPage": null
            }
        }
        """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/search/metadata", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(searchResponseJson)
            });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains($"/assets/{assetId}", StringComparison.OrdinalIgnoreCase) &&
                    !req.RequestUri!.ToString().Contains("/thumbnail", StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(assetInfoJson)
            });

        var mockImageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/thumbnail")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(mockImageData)
            });
    }
}
