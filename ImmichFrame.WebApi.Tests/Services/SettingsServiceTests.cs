using ImmichFrame.Core.Interfaces;
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

            Assert.Multiple(() =>
            {
                Assert.That(service.Current.Accounts, Is.Empty);
                Assert.That(service.Current.GeneralSettings.Interval, Is.EqualTo(new GeneralSettings().Interval));
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

        private class TestDbContextFactory(DbContextOptions<SettingsDbContext> _options) : IDbContextFactory<SettingsDbContext>
        {
            public SettingsDbContext CreateDbContext() => new(_options);
        }
    }
}
