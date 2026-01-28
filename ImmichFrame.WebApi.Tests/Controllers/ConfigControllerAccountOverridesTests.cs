using System.Net;
using System.Net.Http.Json;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers;

[TestFixture]
public class ConfigControllerAccountOverridesTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task PutAccountOverrides_WhenAuthenticationSecretIsSet_RequiresBearerToken()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // isolate DB per test
                    Environment.SetEnvironmentVariable("IMMICHFRAME_DB_PATH",
                        Path.Combine(Path.GetTempPath(), $"immichframe-tests-{Guid.NewGuid()}.db"));

                    var generalSettings = new GeneralSettings { AuthenticationSecret = "secret" };
                    var accountSettings = new ServerAccountSettings
                    {
                        ImmichServerUrl = "http://mock",
                        ApiKey = "k"
                    };
                    var serverSettings = new ServerSettings
                    {
                        GeneralSettingsImpl = generalSettings,
                        AccountsImpl = new List<ServerAccountSettings> { accountSettings }
                    };

                    services.AddSingleton<IServerSettings>(serverSettings);
                    services.AddSingleton<IGeneralSettings>(generalSettings);
                });
            });

        var client = _factory.CreateClient();

        var put = await client.PutAsJsonAsync("/api/Config/account-overrides", new AccountOverrideDto { ShowFavorites = true });
        Assert.That(put.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "secret");
        var putOk = await client.PutAsJsonAsync("/api/Config/account-overrides", new AccountOverrideDto { ShowFavorites = true });
        Assert.That(putOk.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PutAccountOverrides_WhenAuthenticationSecretIsNotSet_DoesNotRequireAuth()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    Environment.SetEnvironmentVariable("IMMICHFRAME_DB_PATH",
                        Path.Combine(Path.GetTempPath(), $"immichframe-tests-{Guid.NewGuid()}.db"));

                    var generalSettings = new GeneralSettings { AuthenticationSecret = null };
                    var accountSettings = new ServerAccountSettings
                    {
                        ImmichServerUrl = "http://mock",
                        ApiKey = "k"
                    };
                    var serverSettings = new ServerSettings
                    {
                        GeneralSettingsImpl = generalSettings,
                        AccountsImpl = new List<ServerAccountSettings> { accountSettings }
                    };

                    services.AddSingleton<IServerSettings>(serverSettings);
                    services.AddSingleton<IGeneralSettings>(generalSettings);
                });
            });

        var client = _factory.CreateClient();
        var put = await client.PutAsJsonAsync("/api/Config/account-overrides", new AccountOverrideDto { ShowFavorites = true });
        Assert.That(put.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}


