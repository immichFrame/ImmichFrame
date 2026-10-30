using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;
using ImmichFrame.WebApi.Database;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Services
{
    [TestFixture]
    public class SettingsServiceTests
    {
        private string _configDir;
        private ILoggerFactory _loggerFactory;

        [SetUp]
        public void Setup()
        {
            _configDir = Path.Combine(Path.GetTempPath(), "immichframe-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_configDir);
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        }

        [TearDown]
        public void TearDown()
        {
            _loggerFactory.Dispose();
            if (Directory.Exists(_configDir))
            {
                Directory.Delete(_configDir, true);
            }
        }

        private SettingsService CreateService()
        {
            var options = new DbContextOptionsBuilder<SettingsDbContext>()
                .UseSqlite($"Data Source={Path.Combine(_configDir, "immichframe.db")}")
                .Options;
            return new SettingsService(
                new TestDbContextFactory(options),
                new ConfigLoader(_loggerFactory.CreateLogger<ConfigLoader>()),
                _loggerFactory.CreateLogger<SettingsService>(),
                new SettingsServiceOptions(_configDir));
        }

        private void WriteSettingsJson(string json)
        {
            File.WriteAllText(Path.Combine(_configDir, "Settings.json"), json);
        }

        private const string ValidV2Json = """
            {
                "General": { "Interval": 10, "AuthenticationSecret": "secret" },
                "Accounts": [ { "ImmichServerUrl": "http://immich.local", "ApiKey": "key123" } ]
            }
            """;

        [Test]
        public async Task InitializeAsync_ImportsFileConfigOnFirstRun()
        {
            WriteSettingsJson(ValidV2Json);

            var service = CreateService();
            await service.InitializeAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.Current.GeneralSettings.Interval, Is.EqualTo(10));
                Assert.That(service.Current.Accounts.Single().ApiKey, Is.EqualTo("key123"));
                Assert.That(File.Exists(Path.Combine(_configDir, "immichframe.db")), Is.True);
            });
        }

        [Test]
        public async Task InitializeAsync_ShuffleModeDefaultsToRandom()
        {
            WriteSettingsJson(ValidV2Json);

            var service = CreateService();
            await service.InitializeAsync();

            Assert.That(service.Current.Accounts.Single().ShuffleMode, Is.EqualTo(ShuffleMode.Random));
        }

        [Test]
        public async Task InitializeAsync_ReadsShuffleMode()
        {
            WriteSettingsJson("""
                {
                    "Accounts": [ { "ImmichServerUrl": "http://immich.local", "ApiKey": "key123", "ShuffleMode": "Exhaustive" } ]
                }
                """);

            var service = CreateService();
            await service.InitializeAsync();

            Assert.That(service.Current.Accounts.Single().ShuffleMode, Is.EqualTo(ShuffleMode.Exhaustive));
        }

        [Test]
        public void ShuffleMode_SurvivesStoredRoundTrip()
        {
            var settings = new ServerSettings
            {
                AccountsImpl = new List<ServerAccountSettings> { new() { ShuffleMode = ShuffleMode.Exhaustive } }
            };

            var json = SettingsService.Serialize(settings);
            var restored = System.Text.Json.JsonSerializer.Deserialize<ServerSettings>(json)!;

            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Contain("\"ShuffleMode\":\"exhaustive\""));
                Assert.That(restored.AccountsImpl.Single().ShuffleMode, Is.EqualTo(ShuffleMode.Exhaustive));
            });
        }

        [Test]
        public void ShuffleMode_ReadsCamelCaseValue()
        {
            // The admin API writes enum values in camelCase.
            const string json = """{ "Accounts": [ { "ShuffleMode": "exhaustive" } ] }""";

            var restored = System.Text.Json.JsonSerializer.Deserialize<ServerSettings>(json)!;

            Assert.That(restored.AccountsImpl.Single().ShuffleMode, Is.EqualTo(ShuffleMode.Exhaustive));
        }

        [TestCase("\"Sometimes\"")]
        [TestCase("1")]
        public async Task InitializeAsync_UnreadableShuffleMode_NotImportedAndSetupStaysClosed(string shuffleModeJson)
        {
            WriteSettingsJson($$"""
                {
                    "General": { "Interval": 10 },
                    "Accounts": [ { "ImmichServerUrl": "http://immich.local", "ApiKey": "key123", "ShuffleMode": {{shuffleModeJson}} } ]
                }
                """);

            var service = CreateService();
            await service.InitializeAsync();
            var adminAuth = new AdminAuthService(service);

            Assert.Multiple(() =>
            {
                Assert.That(service.IsUnconfigured, Is.False);
                Assert.That(service.Current.Accounts, Is.Empty);
                Assert.That(service.Current.GeneralSettings.Interval, Is.EqualTo(new GeneralSettings().Interval));
                if (Environment.GetEnvironmentVariable(AdminAuthService.AdminPasswordEnvVar) == null)
                {
                    Assert.That(adminAuth.SetupRequired, Is.False);
                }
            });

            var options = new DbContextOptionsBuilder<SettingsDbContext>()
                .UseSqlite($"Data Source={Path.Combine(_configDir, "immichframe.db")}")
                .Options;
            await using var db = new SettingsDbContext(options);
            Assert.That(await db.SettingsDocuments.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task InitializeAsync_BrokenYaml_NotImportedAndSetupStaysClosed()
        {
            File.WriteAllText(Path.Combine(_configDir, "Settings.yml"), """
                General:
                  Interval: 10
                Accounts:
                  - ImmichServerUrl: "http://immich.local
                    ApiKey: [key123
                """);

            var service = CreateService();
            await service.InitializeAsync();
            var adminAuth = new AdminAuthService(service);

            Assert.Multiple(() =>
            {
                Assert.That(service.IsUnconfigured, Is.False);
                Assert.That(service.Current.Accounts, Is.Empty);
                if (Environment.GetEnvironmentVariable(AdminAuthService.AdminPasswordEnvVar) == null)
                {
                    Assert.That(adminAuth.SetupRequired, Is.False);
                    Assert.That(adminAuth.State, Is.EqualTo(AdminUiState.Disabled));
                }
            });

            var options = new DbContextOptionsBuilder<SettingsDbContext>()
                .UseSqlite($"Data Source={Path.Combine(_configDir, "immichframe.db")}")
                .Options;
            await using var db = new SettingsDbContext(options);
            Assert.That(await db.SettingsDocuments.CountAsync(), Is.EqualTo(0));
        }

        [TestCase("", null)]
        [TestCase("   ", null)]
        [TestCase(" \t\n", null)]
        [TestCase(null, null)]
        [TestCase("secret", "secret")]
        public void AuthenticationSecret_EmptyOrWhitespaceMeansNoSecret(string? configured, string? expected)
        {
            Assert.That(new GeneralSettings { AuthenticationSecret = configured }.AuthenticationSecret, Is.EqualTo(expected));
        }

        [Test]
        public async Task InitializeAsync_EmptyAuthenticationSecretInFile_IsNoSecret()
        {
            WriteSettingsJson(ValidV2Json.Replace("\"AuthenticationSecret\": \"secret\"", "\"AuthenticationSecret\": \"\""));

            var service = CreateService();
            await service.InitializeAsync();

            Assert.That(service.Current.GeneralSettings.AuthenticationSecret, Is.Null);
        }

        [TestCase("admin-pass", true)]
        [TestCase("admin-pas", false)]
        [TestCase("admin-pass-longer", false)]
        [TestCase("ADMIN-PASS", false)]
        [TestCase("", false)]
        public async Task ValidatePassword_MatchesOnlyTheExactPassword(string candidate, bool expected)
        {
            if (Environment.GetEnvironmentVariable(AdminAuthService.AdminPasswordEnvVar) != null)
            {
                Assert.Ignore("The admin password comes from the environment here.");
            }

            WriteSettingsJson(ValidV2Json.Replace("\"AuthenticationSecret\": \"secret\"", "\"AdminPassword\": \"admin-pass\""));
            var service = CreateService();
            await service.InitializeAsync();

            Assert.That(new AdminAuthService(service).ValidatePassword(candidate), Is.EqualTo(expected));
        }

        [TestCase(null, ShuffleMode.Exhaustive, true)]
        [TestCase("", ShuffleMode.Exhaustive, true)]
        [TestCase("secret", ShuffleMode.Exhaustive, false)]
        [TestCase(null, ShuffleMode.Random, false)]
        [TestCase("secret", ShuffleMode.Random, false)]
        public void ExhaustiveWithoutSecret_OnlyWhenExhaustiveAndNoSecret(string? secret, ShuffleMode mode, bool expected)
        {
            var settings = new ServerSettings
            {
                GeneralSettingsImpl = new GeneralSettings { AuthenticationSecret = secret },
                AccountsImpl = new List<ServerAccountSettings>
                {
                    new() { ShuffleMode = ShuffleMode.Random },
                    new() { ShuffleMode = mode }
                }
            };

            Assert.That(SettingsService.ExhaustiveWithoutSecret(settings), Is.EqualTo(expected));
        }

        [Test]
        public async Task InitializeAsync_DbWinsOverChangedFile()
        {
            WriteSettingsJson(ValidV2Json);
            var first = CreateService();
            await first.InitializeAsync();

            // Change the file afterwards — the DB was already populated and must win
            WriteSettingsJson(ValidV2Json.Replace("\"Interval\": 10", "\"Interval\": 99"));

            var second = CreateService();
            await second.InitializeAsync();

            Assert.That(second.Current.GeneralSettings.Interval, Is.EqualTo(10));
        }

        [Test]
        public async Task InitializeAsync_NoConfig_StartsWithDefaultsAndDoesNotPersist()
        {
            var service = CreateService();
            await service.InitializeAsync();
            var adminAuth = new AdminAuthService(service);

            Assert.Multiple(() =>
            {
                Assert.That(service.Current.Accounts, Is.Empty);
                Assert.That(service.Current.GeneralSettings.Interval, Is.EqualTo(new GeneralSettings().Interval));
                // Only a genuinely fresh install opens anonymous setup.
                Assert.That(service.IsUnconfigured, Is.True);
                if (Environment.GetEnvironmentVariable(AdminAuthService.AdminPasswordEnvVar) == null)
                {
                    Assert.That(adminAuth.SetupRequired, Is.True);
                    Assert.That(adminAuth.State, Is.EqualTo(AdminUiState.Setup));
                }
            });

            var options = new DbContextOptionsBuilder<SettingsDbContext>()
                .UseSqlite($"Data Source={Path.Combine(_configDir, "immichframe.db")}")
                .Options;
            await using var db = new SettingsDbContext(options);
            Assert.That(await db.SettingsDocuments.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task InitializeAsync_ApiKeyFileStaysRawButResolvesInCurrent()
        {
            var keyFile = Path.Combine(_configDir, "apikey.txt");
            await File.WriteAllTextAsync(keyFile, "file-key\n");
            WriteSettingsJson($$"""
                {
                    "General": { "Interval": 10 },
                    "Accounts": [ { "ImmichServerUrl": "http://immich.local", "ApiKeyFile": {{System.Text.Json.JsonSerializer.Serialize(keyFile)}} } ]
                }
                """);

            var service = CreateService();
            await service.InitializeAsync();

            Assert.Multiple(() =>
            {
                // Runtime settings have the resolved key
                Assert.That(service.Current.Accounts.Single().ApiKey, Is.EqualTo("file-key"));
                // Raw settings keep ApiKeyFile unresolved and ApiKey empty (round-trip safe)
                var rawAccount = service.GetRawSettings().AccountsImpl.Single();
                Assert.That(rawAccount.ApiKeyFile, Is.EqualTo(keyFile));
                Assert.That(rawAccount.ApiKey, Is.Empty);
            });

            // A second load from the DB must not trip the "both ApiKey and ApiKeyFile" validation
            var second = CreateService();
            await second.InitializeAsync();
            Assert.That(second.Current.Accounts.Single().ApiKey, Is.EqualTo("file-key"));
        }

        [Test]
        public async Task UpdateAsync_PersistsAndRaisesEventWithFlags()
        {
            var service = CreateService();
            await service.InitializeAsync();

            SettingsChangedEventArgs? received = null;
            service.SettingsChanged += (_, args) => received = args;

            var settings = service.GetRawSettings();
            settings.AccountsImpl = new List<ServerAccountSettings>
            {
                new() { ImmichServerUrl = "http://immich.local", ApiKey = "key123" }
            };
            await service.UpdateAsync(settings);

            Assert.Multiple(() =>
            {
                Assert.That(received, Is.Not.Null);
                Assert.That(received!.AccountsChanged, Is.True);
                Assert.That(service.Current.Accounts.Count(), Is.EqualTo(1));
            });

            // Survives a restart
            var second = CreateService();
            await second.InitializeAsync();
            Assert.That(second.Current.Accounts.Single().ApiKey, Is.EqualTo("key123"));
        }

        [Test]
        public async Task UpdateAsync_GeneralOnlyChange_DoesNotFlagAccounts()
        {
            WriteSettingsJson(ValidV2Json);
            var service = CreateService();
            await service.InitializeAsync();

            SettingsChangedEventArgs? received = null;
            service.SettingsChanged += (_, args) => received = args;

            var settings = service.GetRawSettings();
            settings.GeneralSettingsImpl!.Interval = 60;
            await service.UpdateAsync(settings);

            Assert.Multiple(() =>
            {
                Assert.That(received!.AccountsChanged, Is.False);
                Assert.That(received.GeneralChanged, Is.True);
                Assert.That(service.Current.GeneralSettings.Interval, Is.EqualTo(60));
            });
        }

        [Test]
        public async Task UpdateAsync_InvalidSettings_ThrowsAndKeepsCurrent()
        {
            WriteSettingsJson(ValidV2Json);
            var service = CreateService();
            await service.InitializeAsync();

            var settings = service.GetRawSettings();
            settings.AccountsImpl = new List<ServerAccountSettings>
            {
                new() { ImmichServerUrl = "http://other.local" } // no ApiKey/ApiKeyFile
            };

            Assert.ThrowsAsync<ImmichFrame.Core.Exceptions.SettingsNotValidException>(() => service.UpdateAsync(settings));
            Assert.That(service.Current.Accounts.Single().ApiKey, Is.EqualTo("key123"));
        }

        [Test]
        public async Task TryClaimSetupAsync_OnlyFirstConcurrentClaimWins()
        {
            var service = CreateService();
            await service.InitializeAsync();

            // Mirrors AdminAuthService.SetupRequired: no password anywhere yet.
            bool SetupRequired() => string.IsNullOrWhiteSpace(service.Current.GeneralSettings.AdminPassword)
                && service.IsUnconfigured;

            var results = await Task.WhenAll(
                Enumerable.Range(0, 8).Select(i => Task.Run(() => service.TryClaimSetupAsync($"password-{i}", SetupRequired))));

            Assert.Multiple(() =>
            {
                Assert.That(results.Count(r => r == SetupResult.Claimed), Is.EqualTo(1));
                Assert.That(results.Count(r => r == SetupResult.AlreadyClaimed), Is.EqualTo(7));
                Assert.That(service.Current.GeneralSettings.AdminPassword, Does.StartWith("password-"));
                Assert.That(service.IsUnconfigured, Is.False);
            });
        }

        private class TestDbContextFactory(DbContextOptions<SettingsDbContext> _options) : IDbContextFactory<SettingsDbContext>
        {
            public SettingsDbContext CreateDbContext() => new(_options);
        }
    }
}
