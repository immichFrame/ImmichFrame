using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ImmichFrame.WebApi.Tests.Mocks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers
{
    /// <summary>
    /// End-to-end tests through the real Program.cs pipeline, including the real
    /// SettingsService with a SQLite db in a temp config dir.
    /// </summary>
    [TestFixture]
    [NonParallelizable] // manipulates process-wide environment variables
    public class AdminControllerTests
    {
        private const string AdminPassword = "test-admin-password";

        private string _configDir;
        private WebApplicationFactory<Program> _factory;

        [SetUp]
        public void Setup()
        {
            _configDir = Path.Combine(Path.GetTempPath(), "immichframe-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_configDir);
            Environment.SetEnvironmentVariable("IMMICHFRAME_CONFIG_PATH", _configDir);
            Environment.SetEnvironmentVariable("IMMICHFRAME_ADMIN_PASSWORD", AdminPassword);

            var versionHandler = new Mock<HttpMessageHandler>().WithServerVersion();
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services => services.UseMockHandler(versionHandler));
                });
        }

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
            Environment.SetEnvironmentVariable("IMMICHFRAME_CONFIG_PATH", null);
            Environment.SetEnvironmentVariable("IMMICHFRAME_ADMIN_PASSWORD", null);
            if (Directory.Exists(_configDir))
            {
                Directory.Delete(_configDir, true);
            }
        }

        private HttpClient CreateAdminClient(string? password = AdminPassword)
        {
            var client = _factory.CreateClient();
            if (password != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", password);
            }

            return client;
        }

        private static JsonNode ValidSettingsBody() => JsonNode.Parse("""
            {
                "General": { "Interval": 42 },
                "Accounts": [ { "ImmichServerUrl": "http://mock-immich-server.com", "ApiKey": "key123" } ]
            }
            """)!;

        [Test]
        public async Task GetStatus_IsAnonymousAndReportsSetupDone()
        {
            var client = _factory.CreateClient(); // no auth header

            var response = await client.GetAsync("/api/Admin/Status");

            response.EnsureSuccessStatusCode();
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
            Assert.That((string)json["state"]!, Is.EqualTo("login"));
        }

        [Test]
        public async Task GetSettings_WithoutPassword_Returns401()
        {
            var client = CreateAdminClient(password: null);

            var response = await client.GetAsync("/api/Admin/Settings");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task GetSettings_WithWrongPassword_Returns401()
        {
            var client = CreateAdminClient("wrong-password");

            var response = await client.GetAsync("/api/Admin/Settings");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task WithoutAnyPassword_Returns401AndStatusReportsSetupRequired()
        {
            Environment.SetEnvironmentVariable("IMMICHFRAME_ADMIN_PASSWORD", null);
            var client = CreateAdminClient();

            var settingsResponse = await client.GetAsync("/api/Admin/Settings");
            var statusResponse = await client.GetAsync("/api/Admin/Status");

            Assert.Multiple(async () =>
            {
                Assert.That(settingsResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                statusResponse.EnsureSuccessStatusCode();
                var json = JsonNode.Parse(await statusResponse.Content.ReadAsStringAsync())!;
                Assert.That((string)json["state"]!, Is.EqualTo("setup"));
            });
        }

        [Test]
        public async Task Setup_WithoutAnyPassword_ClaimsInstanceAnonymously()
        {
            Environment.SetEnvironmentVariable("IMMICHFRAME_ADMIN_PASSWORD", null);
            var client = _factory.CreateClient(); // no auth header

            var setupResponse = await client.PostAsJsonAsync("/api/Admin/Setup",
                new { AdminPassword = "chosen-in-onboarding" });
            setupResponse.EnsureSuccessStatusCode();

            var statusResponse = await client.GetAsync("/api/Admin/Status");
            var json = JsonNode.Parse(await statusResponse.Content.ReadAsStringAsync())!;
            Assert.That((string)json["state"]!, Is.EqualTo("login"));

            var authed = CreateAdminClient("chosen-in-onboarding");
            var settingsResponse = await authed.GetAsync("/api/Admin/Settings");
            Assert.That(settingsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task ConfiguredWithoutPassword_ReportsDisabledAndRefusesSetup()
        {
            Environment.SetEnvironmentVariable("IMMICHFRAME_ADMIN_PASSWORD", null);
            // An imported config makes this instance somebody's, even without a password.
            await File.WriteAllTextAsync(Path.Combine(_configDir, "Settings.json"),
                """
                {
                    "General": { "Interval": 99 },
                    "Accounts": [ { "ImmichServerUrl": "http://mock-immich-server.com", "ApiKey": "key123" } ]
                }
                """);

            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.ConfigureTestServices(
                    services => services.UseMockHandler(new Mock<HttpMessageHandler>().WithServerVersion())));
            var client = factory.CreateClient();

            var statusResponse = await client.GetAsync("/api/Admin/Status");
            var json = JsonNode.Parse(await statusResponse.Content.ReadAsStringAsync())!;

            var setupResponse = await client.PostAsJsonAsync("/api/Admin/Setup",
                new { AdminPassword = "hijack-attempt" });

            Assert.Multiple(() =>
            {
                Assert.That((string)json["state"]!, Is.EqualTo("disabled"));
                Assert.That(setupResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            });
        }

        [Test]
        public async Task Setup_WhenAlreadySetUp_Returns409()
        {
            var client = _factory.CreateClient(); // env password is set by Setup()

            var response = await client.PostAsJsonAsync("/api/Admin/Setup",
                new { AdminPassword = "hijack-attempt" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public async Task Setup_WithEmptyPassword_Returns400()
        {
            Environment.SetEnvironmentVariable("IMMICHFRAME_ADMIN_PASSWORD", null);
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Admin/Setup", new { AdminPassword = "  " });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutAndGetSettings_RoundTrips()
        {
            var client = CreateAdminClient();

            var putResponse = await client.PutAsJsonAsync("/api/Admin/Settings", ValidSettingsBody());
            putResponse.EnsureSuccessStatusCode();

            var getResponse = await client.GetAsync("/api/Admin/Settings");
            getResponse.EnsureSuccessStatusCode();
            var json = JsonNode.Parse(await getResponse.Content.ReadAsStringAsync())!;

            Assert.Multiple(() =>
            {
                Assert.That((int)json["General"]!["interval"]!, Is.EqualTo(42));
                Assert.That((string)json["Accounts"]![0]!["apiKey"]!, Is.EqualTo("key123"));
            });
        }

        [Test]
        public async Task PutSettings_Invalid_Returns400()
        {
            var client = CreateAdminClient();

            var body = JsonNode.Parse("""
                {
                    "General": { "Interval": 42 },
                    "Accounts": [ { "ImmichServerUrl": "http://mock-immich-server.com" } ]
                }
                """);
            var response = await client.PutAsJsonAsync("/api/Admin/Settings", body);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutSettings_IsReflectedByClientConfig_WithoutRestart()
        {
            var adminClient = CreateAdminClient();
            var configClient = _factory.CreateClient();

            var before = JsonNode.Parse(await configClient.GetStringAsync("/api/Config"))!;
            Assert.That((int)before["interval"]!, Is.Not.EqualTo(42));

            var putResponse = await adminClient.PutAsJsonAsync("/api/Admin/Settings", ValidSettingsBody());
            putResponse.EnsureSuccessStatusCode();

            var after = JsonNode.Parse(await configClient.GetStringAsync("/api/Config"))!;
            Assert.That((int)after["interval"]!, Is.EqualTo(42));
        }

        [Test]
        public async Task TestAccount_ReturnsSuccessForReachableServer()
        {
            var client = CreateAdminClient();

            var body = JsonNode.Parse("""
                { "ImmichServerUrl": "http://mock-immich-server.com", "ApiKey": "key123" }
                """);
            var response = await client.PostAsJsonAsync("/api/Admin/Settings/TestAccount", body);

            response.EnsureSuccessStatusCode();
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
            Assert.That((bool)json["success"]!, Is.True);
        }
    }
}
