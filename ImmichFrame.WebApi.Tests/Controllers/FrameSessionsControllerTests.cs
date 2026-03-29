using System.Net;
using System.Net.Http.Json;
using System.Collections;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers;

[TestFixture]
public class FrameSessionsControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private string _tempAppDataPath = null!;

    [SetUp]
    public void Setup()
    {
        _tempAppDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempAppDataPath);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var generalSettings = new GeneralSettings
                    {
                        AuthenticationSecret = "test-secret",
                        PhotoDateFormat = "MM/dd/yyyy",
                        ImageLocationFormat = "City,State,Country",
                        Language = "en"
                    };

                    var accountSettings = new ServerAccountSettings
                    {
                        ImmichServerUrl = "http://mock-immich-server.com",
                        ApiKey = "test-api-key"
                    };

                    var serverSettings = new ServerSettings
                    {
                        GeneralSettingsImpl = generalSettings,
                        AccountsImpl = new List<ServerAccountSettings> { accountSettings }
                    };

                    services.AddSingleton<IServerSettings>(serverSettings);
                    services.AddSingleton<IGeneralSettings>(generalSettings);
                    services.AddSingleton<IAdminBasicAuthService>(_ =>
                        new AdminBasicAuthService(new Hashtable
                        {
                            ["IMMICHFRAME_AUTH_BASIC_ADMIN_USER"] = "admin",
                            ["IMMICHFRAME_AUTH_BASIC_ADMIN_HASH"] =
                                "{SHA}" + Convert.ToBase64String(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes("secret")))
                        }));
                    services.AddSingleton<IFrameSessionRegistry>(_ =>
                        new FrameSessionRegistry(
                            new FrameSessionRegistryOptions
                            {
                                DisplayNameStorePath = Path.Combine(_tempAppDataPath, "frame-session-display-names.json")
                            },
                            null,
                            null));
                });
            });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
        if (Directory.Exists(_tempAppDataPath))
        {
            Directory.Delete(_tempAppDataPath, true);
        }
    }

    [Test]
    public async Task AdminEndpoints_RequireAuthentication()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/admin/frame-sessions");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AdminAuthSession_ReturnsAuthenticatedUserAfterLogin()
    {
        var adminClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var sessionBeforeLogin = await adminClient.GetFromJsonAsync<AdminAuthSessionDto>("/api/admin/auth/session");
        Assert.That(sessionBeforeLogin, Is.Not.Null);
        Assert.That(sessionBeforeLogin!.IsConfigured, Is.True);
        Assert.That(sessionBeforeLogin.IsAuthenticated, Is.False);

        await LoginAdminAsync(adminClient);

        var sessionAfterLogin = await adminClient.GetFromJsonAsync<AdminAuthSessionDto>("/api/admin/auth/session");
        Assert.That(sessionAfterLogin, Is.Not.Null);
        Assert.That(sessionAfterLogin!.IsAuthenticated, Is.True);
        Assert.That(sessionAfterLogin.Username, Is.EqualTo("admin"));
    }

    [Test]
    public async Task SessionLifecycle_ReturnsActiveSessionsAndCommands()
    {
        var frameClient = _factory.CreateClient();
        frameClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-secret");

        var snapshot = new FrameSessionSnapshotDto
        {
            PlaybackState = FramePlaybackState.Playing,
            Status = FrameSessionStatus.Active,
            CurrentDisplay = new DisplayEventDto
            {
                DisplayedAtUtc = DateTimeOffset.UtcNow,
                DurationSeconds = 20,
                Assets =
                [
                    new DisplayedAssetDto
                    {
                        Id = "asset-1",
                        OriginalFileName = "current.jpg",
                        Type = ImmichFrame.Core.Api.AssetTypeEnum.IMAGE,
                        ImmichServerUrl = "http://mock-immich-server.com"
                    }
                ]
            }
        };

        var putResponse = await frameClient.PutAsJsonAsync("/api/frame-sessions/frame-1", snapshot);
        putResponse.EnsureSuccessStatusCode();

        var adminClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await LoginAdminAsync(adminClient);

        var adminResponse = await adminClient.GetFromJsonAsync<List<FrameSessionStateDto>>("/api/admin/frame-sessions");

        Assert.That(adminResponse, Is.Not.Null);
        Assert.That(adminResponse!, Has.Count.EqualTo(1));
        Assert.That(adminResponse[0].ClientIdentifier, Is.EqualTo("frame1"));
        Assert.That(adminResponse[0].CurrentDisplay?.Assets[0].OriginalFileName, Is.EqualTo("current.jpg"));
        Assert.That(adminResponse[0].CurrentDisplay?.Assets[0].ImmichServerUrl, Is.EqualTo("http://mock-immich-server.com"));
        Assert.That(adminResponse[0].CurrentDisplay?.DurationSeconds, Is.EqualTo(20));

        var commandResponse = await adminClient.PostAsJsonAsync("/api/admin/frame-sessions/frame-1/commands", new EnqueueAdminCommandRequest
        {
            CommandType = FrameAdminCommandType.Next
        });
        commandResponse.EnsureSuccessStatusCode();

        var commands = await frameClient.GetFromJsonAsync<List<AdminCommandDto>>("/api/frame-sessions/frame-1/commands");

        Assert.That(commands, Is.Not.Null);
        Assert.That(commands!, Has.Count.EqualTo(1));
        Assert.That(commands[0].CommandType, Is.EqualTo(FrameAdminCommandType.Next));

        var ackResponse = await frameClient.PostAsync($"/api/frame-sessions/frame-1/commands/{commands[0].CommandId}/ack", null);
        ackResponse.EnsureSuccessStatusCode();

        var commandsAfterAck = await frameClient.GetFromJsonAsync<List<AdminCommandDto>>("/api/frame-sessions/frame-1/commands");
        Assert.That(commandsAfterAck, Is.Empty);
    }

    [Test]
    public async Task EnqueueCommand_ReturnsGoneForStaleSession()
    {
        var registry = _factory.Services.GetRequiredService<IFrameSessionRegistry>();
        registry.UpsertSnapshot("framestale", new FrameSessionSnapshotDto(), "NUnit-Agent");
        registry.MarkStopped("framestale", "NUnit-Agent");

        var adminClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await LoginAdminAsync(adminClient);

        var response = await adminClient.PostAsJsonAsync(
            "/api/admin/frame-sessions/framestale/commands",
            new EnqueueAdminCommandRequest
            {
                CommandType = FrameAdminCommandType.Refresh
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Gone));
    }

    private static async Task LoginAdminAsync(HttpClient adminClient)
    {
        var loginResponse = await adminClient.PostAsJsonAsync("/api/admin/auth/login", new AdminLoginRequest
        {
            Username = "admin",
            Password = "secret"
        });

        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await loginResponse.Content.ReadAsStringAsync());
    }
}
