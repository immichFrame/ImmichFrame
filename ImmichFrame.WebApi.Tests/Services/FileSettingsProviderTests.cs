using ImmichFrame.Core.Exceptions;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Services
{
    [TestFixture]
    public class FileSettingsTests
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

        private SettingsService Create(string? configDir = null)
        {
            return new SettingsService(
                null,
                new ConfigLoader(_loggerFactory.CreateLogger<ConfigLoader>()),
                _loggerFactory.CreateLogger<SettingsService>(),
                new SettingsServiceOptions(configDir ?? _configDir, SettingsSource.File));
        }

        [Test]
        public async Task InitializeAsync_LoadsFileAndDoesNotCreateDatabase()
        {
            await File.WriteAllTextAsync(Path.Combine(_configDir, "Settings.yml"), """
                General:
                  Interval: 17
                Accounts:
                  - ImmichServerUrl: http://immich.local
                    ApiKey: key123
                """);

            var provider = Create();
            await provider.InitializeAsync();

            Assert.Multiple(() =>
            {
                Assert.That(provider.Current.GeneralSettings.Interval, Is.EqualTo(17));
                Assert.That(provider.Current.Accounts.Single().ApiKey, Is.EqualTo("key123"));
                Assert.That(File.Exists(Path.Combine(_configDir, "immichframe.db")), Is.False);
            });
        }

        [Test]
        public async Task InitializeAsync_LeavesExistingDatabaseUntouched()
        {
            await File.WriteAllTextAsync(Path.Combine(_configDir, "Settings.json"), """
                {
                    "General": { "Interval": 10 },
                    "Accounts": [ { "ImmichServerUrl": "http://immich.local", "ApiKey": "key123" } ]
                }
                """);
            var dbPath = Path.Combine(_configDir, "immichframe.db");
            var original = new byte[] { 1, 2, 3, 4 };
            await File.WriteAllBytesAsync(dbPath, original);

            await Create().InitializeAsync();

            Assert.That(await File.ReadAllBytesAsync(dbPath), Is.EqualTo(original));
        }

        [Test]
        public void InitializeAsync_MissingFile_ThrowsAndDoesNotCreateDirectory()
        {
            var missing = Path.Combine(Path.GetTempPath(), "immichframe-tests", Guid.NewGuid().ToString());

            var ex = Assert.ThrowsAsync<ImmichFrameException>(() => Create(missing).InitializeAsync());

            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Does.Contain(SettingsSourceParser.EnvVar));
                Assert.That(Directory.Exists(missing), Is.False);
            });
        }

        [Test]
        public async Task InitializeAsync_InvalidFile_ThrowsAndDoesNotCreateDatabase()
        {
            await File.WriteAllTextAsync(Path.Combine(_configDir, "Settings.json"), """
                {
                    "Accounts": [ { "ImmichServerUrl": "http://immich.local" } ]
                }
                """);

            Assert.ThrowsAsync<ImmichFrameException>(() => Create().InitializeAsync());
            Assert.That(File.Exists(Path.Combine(_configDir, "immichframe.db")), Is.False);
        }

        [Test]
        public async Task InitializeAsync_MultipleFiles_ThrowsAndLeavesDatabaseUntouched()
        {
            await File.WriteAllTextAsync(Path.Combine(_configDir, "Settings.json"), """
                {
                    "General": { "Interval": 10 },
                    "Accounts": [ { "ImmichServerUrl": "http://immich.local", "ApiKey": "from-json" } ]
                }
                """);
            await File.WriteAllTextAsync(Path.Combine(_configDir, "Settings.yml"), """
                General:
                  Interval: 17
                Accounts:
                  - ImmichServerUrl: http://immich.local
                    ApiKey: from-yml
                """);
            var dbPath = Path.Combine(_configDir, "immichframe.db");
            var original = new byte[] { 1, 2, 3, 4 };
            await File.WriteAllBytesAsync(dbPath, original);

            var ex = Assert.ThrowsAsync<ImmichFrameException>(() => Create().InitializeAsync());
            var bytes = await File.ReadAllBytesAsync(dbPath);

            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Does.Contain("Settings.json"));
                Assert.That(ex.Message, Does.Contain("Settings.yml"));
                Assert.That(bytes, Is.EqualTo(original));
            });
        }

        [Test]
        public async Task InitializeAsync_UnknownJsonKey_ThrowsAndDoesNotCreateDatabase()
        {
            await File.WriteAllTextAsync(Path.Combine(_configDir, "Settings.json"), """
                {
                    "General": { "Interval": 10, "NotARealSetting": true },
                    "Accounts": [ { "ImmichServerUrl": "http://immich.local", "ApiKey": "key123" } ]
                }
                """);

            var ex = Assert.ThrowsAsync<ImmichFrameException>(() => Create().InitializeAsync());

            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Does.Contain("invalid"));
                Assert.That(ex.Message, Does.Contain("NotARealSetting"));
                Assert.That(File.Exists(Path.Combine(_configDir, "immichframe.db")), Is.False);
            });
        }

        [Test]
        public async Task InitializeAsync_UnknownYamlKey_ThrowsAndDoesNotCreateDatabase()
        {
            await File.WriteAllTextAsync(Path.Combine(_configDir, "Settings.yml"), """
                General:
                  Interval: 10
                  NotARealSetting: true
                Accounts:
                  - ImmichServerUrl: http://immich.local
                    ApiKey: key123
                """);

            var ex = Assert.ThrowsAsync<ImmichFrameException>(() => Create().InitializeAsync());

            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Does.Contain("invalid"));
                Assert.That(ex.Message, Does.Contain("NotARealSetting"));
                Assert.That(File.Exists(Path.Combine(_configDir, "immichframe.db")), Is.False);
            });
        }

        [Test]
        public void Parse_RejectsUnknownValue()
        {
            var ex = Assert.Throws<ImmichFrameException>(() => SettingsSourceParser.Parse("yaml"));
            Assert.That(ex!.Message, Does.Contain("yaml"));
        }

        [Test]
        public void Parse_DefaultsToDatabase()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SettingsSourceParser.Parse(null), Is.EqualTo(SettingsSource.Database));
                Assert.That(SettingsSourceParser.Parse("  "), Is.EqualTo(SettingsSource.Database));
                Assert.That(SettingsSourceParser.Parse("FILE"), Is.EqualTo(SettingsSource.File));
            });
        }

    }
}
