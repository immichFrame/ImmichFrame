using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic.Pool;

/// <summary>
/// The real pool chain Exhaustive mode builds for an account showing its library and memories,
/// drawn one asset at a time the way a frame does.
/// </summary>
[TestFixture]
public class ExhaustiveChainTests
{
    private const int LibrarySize = 30;
    private const int MemorySize = 6;
    private const string ClientId = "frame-1";

    private Mock<ImmichApi> _api;
    private Mock<IAccountSettings> _settings;
    private ApiCache _cache;
    private List<Guid> _libraryIds;
    private List<Guid> _memoryIds;
    private int _listings;

    [SetUp]
    public void Setup()
    {
        _libraryIds = Enumerable.Range(0, LibrarySize).Select(i => FixtureHelpers.GuidFor($"library_{i}")).ToList();

        _api = new Mock<ImmichApi>(null, null);
        _api.Setup(a => a.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchStatisticsResponseDto { Total = LibrarySize });
        _api.Setup(a => a.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string slug, MetadataSearchDto dto, CancellationToken ct) =>
            {
                // Without an ID filter it is the listing; with one, loading a dealt asset.
                if (dto.Filter?.Id == null)
                {
                    Interlocked.Increment(ref _listings);
                    return AccountSearchPoolTests.SearchPage(_libraryIds, null);
                }

                return AccountSearchPoolTests.SearchPage(new List<Guid> { dto.Filter.Id.Eq!.Value }, null);
            });
        _api.Setup(a => a.GetAssetInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, string key, string slug, CancellationToken ct) => new AssetResponseDto
            {
                Id = id,
                Type = AssetTypeEnum.IMAGE,
                IsTrashed = false,
                ExifInfo = new ExifResponseDto()
            });

        var memories = Enumerable.Range(0, 2).Select(m => new MemoryResponseDto
        {
            Id = FixtureHelpers.GuidFor($"memory_{m}"),
            Data = new OnThisDayDto { Year = DateTime.Now.Year - 1 - m },
            Assets = Enumerable.Range(0, MemorySize / 2).Select(i => new AssetResponseDto
            {
                Id = FixtureHelpers.GuidFor($"memory_{m}_{i}"),
                Type = AssetTypeEnum.IMAGE,
                // With EXIF present the memory pool doesn't look the asset up again.
                ExifInfo = new ExifResponseDto(),
                People = new List<PersonResponseDto>()
            }).ToList()
        }).ToList();
        _memoryIds = memories.SelectMany(m => m.Assets).Select(a => a.Id).ToList();
        _api.Setup(a => a.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);

        _settings = new Mock<IAccountSettings>();
        _settings.SetupGet(s => s.ImmichServerUrl).Returns("https://photos.example");
        _settings.SetupGet(s => s.ShowMemories).Returns(true);
        _settings.SetupGet(s => s.Albums).Returns(new List<Guid>());
        _settings.SetupGet(s => s.People).Returns(new List<Guid>());
        _settings.SetupGet(s => s.Tags).Returns(new List<string>());
        _settings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid>());

        _cache = new ApiCache(TimeSpan.FromHours(1));
    }

    [TearDown]
    public void TearDown() => _cache.Dispose();

    [Test]
    public async Task OneAtATime_EachSourceCoversItsFirstCycleWithoutRepeat()
    {
        var pool = new MultiAssetPool(new List<IAssetPool>
        {
            new ShuffleDeckPool(new AccountSearchPool(_cache, _api.Object, _settings.Object)),
            new ShuffleDeckPool(new MemoryAssetsPool(_api.Object, _settings.Object))
        });

        var fromLibrary = new List<Guid>();
        var fromMemories = new List<Guid>();
        for (var draw = 0; draw < 10_000 && (fromLibrary.Count < LibrarySize || fromMemories.Count < MemorySize); draw++)
        {
            var asset = (await pool.GetAssets(1, ClientId)).Single();
            if (_libraryIds.Contains(asset.Id))
            {
                fromLibrary.Add(asset.Id);
            }
            else if (_memoryIds.Contains(asset.Id))
            {
                fromMemories.Add(asset.Id);
            }
            else
            {
                Assert.Fail($"Asset {asset.Id} came from neither source");
            }
        }

        Assert.That(fromLibrary, Has.Count.GreaterThanOrEqualTo(LibrarySize));
        Assert.That(fromMemories, Has.Count.GreaterThanOrEqualTo(MemorySize));
        Assert.Multiple(() =>
        {
            Assert.That(fromLibrary.Take(LibrarySize), Is.Unique);
            Assert.That(fromLibrary.Take(LibrarySize), Is.EquivalentTo(_libraryIds));
            Assert.That(fromMemories.Take(MemorySize), Is.Unique);
            Assert.That(fromMemories.Take(MemorySize), Is.EquivalentTo(_memoryIds));
        });

        // The library is listed once through the metadata search and never picked at random.
        Assert.That(_listings, Is.EqualTo(1));
        _api.Verify(a => a.SearchRandomAsync(It.IsAny<RandomSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
