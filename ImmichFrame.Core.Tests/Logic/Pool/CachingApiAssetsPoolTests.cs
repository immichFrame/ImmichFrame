using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class CachingApiAssetsPoolTests
{
    private Mock<IApiCache> _mockApiCache;
    private TestableCachingApiAssetsPool _testPool;

    // Concrete implementation for testing the abstract class
    private class TestableCachingApiAssetsPool : CachingApiAssetsPool
    {
        public Func<Task<IEnumerable<AssetResponseDto>>> LoadAssetsFunc { get; set; }

        public TestableCachingApiAssetsPool(IApiCache apiCache)
            : base(apiCache)
        {
        }

        protected override Task<IEnumerable<AssetResponseDto>> LoadAssets(CancellationToken ct = default)
        {
            return LoadAssetsFunc != null ? LoadAssetsFunc() : Task.FromResult(Enumerable.Empty<AssetResponseDto>());
        }
    }

    [SetUp]
    public void Setup()
    {
        _mockApiCache = new Mock<IApiCache>();
        _testPool = new TestableCachingApiAssetsPool(_mockApiCache.Object);

        // Default setup for ApiCache to execute the factory function
        _mockApiCache.Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<AssetResponseDto>>>>()
            ))
            .Returns<string, Func<Task<IEnumerable<AssetResponseDto>>>>(async (key, factory) => await factory());
    }

    private List<AssetResponseDto> CreateSampleAssets()
    {
        return new List<AssetResponseDto>
        {
            new AssetResponseDto { Id = FixtureHelpers.GuidFor("1"), Type = AssetTypeEnum.IMAGE, IsArchived = false, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddDays(-10), Rating = 5 } },
            new AssetResponseDto { Id = FixtureHelpers.GuidFor("2"), Type = AssetTypeEnum.VIDEO, IsArchived = false, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddDays(-10) } }, // Video asset
            new AssetResponseDto { Id = FixtureHelpers.GuidFor("3"), Type = AssetTypeEnum.IMAGE, IsArchived = true, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddDays(-5), Rating = 3 } }, // Potentially filtered by archive status
            new AssetResponseDto { Id = FixtureHelpers.GuidFor("4"), Type = AssetTypeEnum.IMAGE, IsArchived = false, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddDays(-2), Rating = 5 } },
            new AssetResponseDto { Id = FixtureHelpers.GuidFor("5"), Type = AssetTypeEnum.IMAGE, IsArchived = false, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddYears(-1), Rating = 1 } },
        };
    }

    [Test]
    public async Task GetAssetCount_ReturnsLoadedCount()
    {
        var assets = CreateSampleAssets();
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);

        var count = await _testPool.GetAssetCount();

        Assert.That(count, Is.EqualTo(assets.Count));
    }

    [Test]
    public async Task GetAssets_ReturnsRequestedNumberOfAssets()
    {
        // Arrange
        var assets = CreateSampleAssets();
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);

        // Act
        var result = (await _testPool.GetAssets(2)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        // All returned assets should be supported media types (image/video)
        Assert.That(result.All(a => a.Type == AssetTypeEnum.IMAGE || a.Type == AssetTypeEnum.VIDEO));
    }

    [Test]
    public async Task GetAssets_ReturnsAllAvailableIfLessThanRequested()
    {
        // Arrange
        var assets = CreateSampleAssets().Where(a => !a.IsArchived).ToList();
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);

        var result = (await _testPool.GetAssets(5)).ToList();

        Assert.That(result.Count, Is.EqualTo(assets.Count));
    }


    [Test]
    public async Task AllAssets_UsesCache_LoadAssetsCalledOnce()
    {
        // Arrange
        var assets = CreateSampleAssets();
        var loadAssetsCallCount = 0;
        _testPool.LoadAssetsFunc = () =>
        {
            loadAssetsCallCount++;
            return Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        };

        // Setup cache to really cache after the first call
        Dictionary<string, IEnumerable<AssetResponseDto>> cacheStore = new();
        _mockApiCache.Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<AssetResponseDto>>>>()
            ))
            .Returns<string, Func<Task<IEnumerable<AssetResponseDto>>>>(async (key, factory) =>
            {
                if (!cacheStore.ContainsKey(key))
                {
                    cacheStore[key] = await factory();
                }

                return cacheStore[key];
            });

        // Act
        await _testPool.GetAssetCount(); // First call, should trigger LoadAssets
        await _testPool.GetAssetCount(); // Second call, should use cache
        await _testPool.GetAssets(1); // Third call, should use cache

        // Assert
        Assert.That(loadAssetsCallCount, Is.EqualTo(1), "LoadAssets should only be called once.");
    }
}
