using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Logic.Pool;
using ImmichFrame.Core.Tests.Logic.Pool;
using ImmichFrame.WebApi.Controllers;
using ImmichFrame.WebApi.Models;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Logic;

[TestFixture]
public class MemoryAssetsPoolTests
{
    private Mock<ImmichApi> _mockImmichApi;
    private Mock<IAccountSettings> _mockAccountSettings;
    private MemoryAssetsPool _memoryAssetsPool;
    private IImmichFrameLogic _accountLogic;


    [SetUp]
    public void Setup()
    {
        _mockImmichApi = new Mock<ImmichApi>(null!, null!); // Base constructor requires ILogger, IHttpClientFactory, IOptions, pass null
        _mockAccountSettings = new Mock<IAccountSettings>();

        _memoryAssetsPool = new MemoryAssetsPool(_mockImmichApi.Object, _mockAccountSettings.Object);
        _accountLogic = new LogicPoolAdapter(_memoryAssetsPool, _mockImmichApi.Object, null);
    }

    private List<AssetResponseDto> CreateSampleAssets(int count, bool withExif, DateTime created)
    {
        var assets = new List<AssetResponseDto>();
        for (int i = 0; i < count; i++)
        {
            var asset = new AssetResponseDto
            {
                Id = Guid.NewGuid().ToString(),
                OriginalPath = $"/path/to/image{i}.jpg",
                Type = AssetTypeEnum.IMAGE,
                ExifInfo = withExif ? new ExifResponseDto { DateTimeOriginal = created } : null,
                People = new List<PersonWithFacesResponseDto>()
            };
            assets.Add(asset);
        }

        return assets;
    }

    private List<MemoryResponseDto> CreateSampleMemories(int memoryCount, int assetsPerMemory, bool withExifInAssets, int memoryYear)
    {
        var memoryAt = DateTimeOffset.Now;
        memoryAt = memoryAt.AddYears(memoryYear - memoryAt.Year);

        var memories = new List<MemoryResponseDto>();
        for (int i = 0; i < memoryCount; i++)
        {
            var memory = new MemoryResponseDto
            {
                Id = $"Memory {i}",
                Assets = CreateSampleAssets(assetsPerMemory, withExifInAssets, new DateTime(memoryYear, 1, 1)),
                MemoryAt = memoryAt,
                Data = new OnThisDayDto { Year = memoryYear }
            };
            memories.Add(memory);
        }

        return memories;
    }

    [Test]
    public async Task LoadAssets_CallsSearchMemoriesAsync()
    {
        // Arrange
        _mockImmichApi.Setup(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MemoryResponseDto>());

        // Act
        // Access protected method via reflection for testing, or make it internal/public if design allows
        // For now, we assume LoadAssets is implicitly called by a public method of CachingApiAssetsPool (e.g. GetAsset)
        // Let's simulate this by calling a method that would trigger LoadAssets if cache is empty.
        // Since LoadAssets is protected, we'll test its effects via GetAsset.
        // We need to ensure the cache is empty or expired for LoadAssets to be called.
        await _memoryAssetsPool.GetAssets(1, CancellationToken.None); // This should trigger LoadAssets

        // Assert
        _mockImmichApi.Verify(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_FetchesAssetInfo_WhenExifInfoIsNull()
    {
        // Arrange
        var memoryYear = DateTime.Now.Year - 2;
        var memories = CreateSampleMemories(1, 1, false, memoryYear); // Asset without ExifInfo
        var assetId = memories[0].Assets.First().Id;

        _mockImmichApi.Setup(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);
        _mockImmichApi.Setup(x => x.GetAssetInfoAsync(new Guid(assetId), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetResponseDto { Id = assetId, ExifInfo = new ExifResponseDto { DateTimeOriginal = new DateTime(memoryYear, 1, 1) }, People = new List<PersonWithFacesResponseDto>() });

        // Act
        var resultAsset = (await _memoryAssetsPool.GetAssets(1, CancellationToken.None)).First(); // Triggers LoadAssets

        // Assert
        _mockImmichApi.Verify(x => x.GetAssetInfoAsync(new Guid(assetId), null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(resultAsset.ExifInfo, Is.Not.Null);
        Assert.That(resultAsset.ExifInfo.Description, Is.EqualTo("2 years ago"));
    }

    [Test]
    public async Task LoadAssets_DoesNotFetchAssetInfo_WhenExifInfoIsPresent()
    {
        // Arrange
        var memoryYear = DateTime.Now.Year - 1;
        var memories = CreateSampleMemories(1, 1, true, memoryYear); // Asset with ExifInfo
        var assetId = memories[0].Assets.First().Id;

        _mockImmichApi.Setup(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);

        // Act
        var resultAsset = (await _memoryAssetsPool.GetAssets(1, CancellationToken.None)).First(); // Triggers LoadAssets

        // Assert
        _mockImmichApi.Verify(x => x.GetAssetInfoAsync(It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()), Times.Never);
        Assert.That(resultAsset.ExifInfo, Is.Not.Null);
        Assert.That(resultAsset.ExifInfo.Description, Is.EqualTo("1 year ago"));
    }

    [Test]
    public async Task LoadAssets_CorrectlyFormatsDescription_YearsAgo()
    {
        // Arrange
        var currentYear = DateTime.Now.Year;
        var testCases = new[]
        {
            new { year = currentYear - 1, expectedDesc = "1 year ago" },
            new { year = currentYear - 5, expectedDesc = "5 years ago" },
            new { year = currentYear, expectedDesc = "0 years ago" } // Or "This year" depending on desired logic, current is "0 years ago"
        };

        foreach (var tc in testCases)
        {
            var memories = CreateSampleMemories(1, 1, true, tc.year);
            memories[0].Assets.First().ExifInfo.DateTimeOriginal = new DateTime(tc.year, 1, 1); // Ensure Exif has the year

            _mockImmichApi.Setup(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(memories);

         _memoryAssetsPool = new MemoryAssetsPool(_mockImmichApi.Object, _mockAccountSettings.Object);


            // Act
            var resultAsset = (await _memoryAssetsPool.GetAssets(1, CancellationToken.None)).First(); // Triggers LoadAssets

            // Assert
            Assert.That(resultAsset.ExifInfo, Is.Not.Null);
            Assert.That(resultAsset.ExifInfo.Description, Is.EqualTo(tc.expectedDesc), $"Failed for year {tc.year}");
        }
    }

    [Test]
    public async Task LoadAssets_AggregatesAssetsFromMultipleMemories()
    {
        // Arrange
        var memoryYear = DateTime.Now.Year - 3;
        var memories = CreateSampleMemories(2, 2, true, memoryYear); // 2 memories, 2 assets each

        _mockImmichApi.Setup(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories).Verifiable(Times.Once);

        // Act
        // We will rely on the fact that the factory in GetFromCacheAsync is called, and it returns the list.
        // The count can be indirectly verified if we could access the pool's internal list after LoadAssets.

        // Let's refine the test to ensure LoadAssets returns the correct number of assets.
        // We need a way to inspect the result of LoadAssets directly.
        // We can make LoadAssets internal and use InternalsVisibleTo, or use reflection.
        // Or, we can rely on the setup of GetFromCacheAsync to capture the factory's result.
        var loadedAssets = await _memoryAssetsPool.GetAssets(4, CancellationToken.None); // Trigger load

        // Assert
        Assert.That(loadedAssets, Is.Not.Null);
        Assert.That(loadedAssets.Count(), Is.EqualTo(4)); // 2 memories * 2 assets
        _mockImmichApi.VerifyAll();
    }

    [Test]
    public async Task LoadAssets_ReturnsAssetsInChronologicalMemoryOrder()
    {
        // Arrange
        var memories = new List<MemoryResponseDto>
        {
            CreateSampleMemories(1, 1, true, DateTime.Now.Year - 2).First(), // 2 years ago
            CreateSampleMemories(1, 1, true, DateTime.Now.Year - 1).First() // 1 year ago
        };

        // Reverse memories to ensure they are not in order by default
        memories.Reverse();

        _mockImmichApi.Setup(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);

        // Act
        var loadedAssets = await _memoryAssetsPool.GetAssets(2, CancellationToken.None); // Trigger load

        // Assert
        Assert.That(loadedAssets, Is.Not.Null);
        Assert.That(loadedAssets.Count(), Is.EqualTo(2));
        // Expecting assets to be sorted by memory date, oldest first
        Assert.That(loadedAssets.First().ExifInfo.Description, Is.EqualTo("2 years ago"));
        Assert.That(loadedAssets.Last().ExifInfo.Description, Is.EqualTo("1 year ago"));
    }

    [Test]
    [Repeat(10)] // In case it accidentally passes due to random order
    public async Task LoadAssetsFromApi_ReturnsAssetsInChronologicalMemoryOrder()
    {
        // Arrange
        var memories = new List<MemoryResponseDto>
        {
            CreateSampleMemories(1, 1, true, DateTime.Now.Year - 2).First(), // 2 years ago
            CreateSampleMemories(1, 1, true, DateTime.Now.Year - 1).First() // 1 year ago
        };

        // Shuffle memories to ensure they are not in order by default
        memories = memories.OrderBy(x => Guid.NewGuid()).ToList();

        _mockImmichApi.Setup(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);

        // Act - test the memory pool directly since that's what we're testing
        var loadedAssets = await _memoryAssetsPool.GetAssets(2, CancellationToken.None);

        // Assert
        Assert.That(loadedAssets, Is.Not.Null);
        Assert.That(loadedAssets.Count(), Is.EqualTo(2));
        // Expecting assets to be sorted by memory date, oldest first
        Assert.That(loadedAssets.First().ExifInfo.Description, Is.EqualTo("2 years ago"));
        Assert.That(loadedAssets.Last().ExifInfo.Description, Is.EqualTo("1 year ago"));
    }

    [Test]
    public async Task LoadAssetsFromApi_ReturnsAssetsInChronologicalMemoryOrderOverMultipleCalls()
    {
        // Arrange
        var memories = new List<MemoryResponseDto>
        {
            CreateSampleMemories(1, 1, true, DateTime.Now.Year - 2).First(), // 2 years ago
            CreateSampleMemories(1, 1, true, DateTime.Now.Year - 1).First() // 1 year ago
        };

        // Shuffle memories to ensure they are not in order by default
        memories = memories.OrderBy(x => Guid.NewGuid()).ToList();

        _mockImmichApi.Setup(x => x.SearchMemoriesAsync(It.IsAny<DateTimeOffset>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);


        // Act
        var loadedAsset1 = await _accountLogic.GetNextAsset(); // Trigger load
        var loadedAsset2 = await _accountLogic.GetNextAsset();

        // Assert
        Assert.That(loadedAsset1, Is.Not.Null);
        Assert.That(loadedAsset2, Is.Not.Null);
        
        // Expecting assets to be sorted by memory date, oldest first
        Assert.That(loadedAsset1.ExifInfo.Description, Is.EqualTo("2 years ago"));
        Assert.That(loadedAsset2.ExifInfo.Description, Is.EqualTo("1 year ago"));
    }
}