using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class CachingApiAssetsPoolTests
{
    private Mock<IApiCache> _mockApiCache;
    private Mock<ImmichApi> _mockImmichApi; // Dependency for constructor, may not be used directly in base class tests
    private Mock<IAccountSettings> _mockAccountSettings;
    private TestableCachingApiAssetsPool _testPool;

    // Concrete implementation for testing the abstract class
    private class TestableCachingApiAssetsPool : CachingApiAssetsPool
    {
        public Func<Task<IEnumerable<AssetResponseDto>>>? LoadAssetsFunc { get; set; }

        public TestableCachingApiAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings)
            : base(apiCache, immichApi, accountSettings)
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
        _mockApiCache = new Mock<IApiCache>(); // ILogger, IOptions<AppSettings>
        _mockImmichApi = new Mock<ImmichApi>("", null!); // ILogger, IHttpClientFactory, IOptions<AppSettings>
        _mockAccountSettings = new Mock<IAccountSettings>();

        _testPool = new TestableCachingApiAssetsPool(_mockApiCache.Object, _mockImmichApi.Object, _mockAccountSettings.Object);

        // Default setup for ApiCache to execute the factory function
        _mockApiCache.Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<AssetResponseDto>>>>()
            ))
            .Returns<string, Func<Task<IEnumerable<AssetResponseDto>>>>(async (key, factory) => await factory());

        // Default account settings
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(true);
        _mockAccountSettings.SetupGet(s => s.ImagesFromDate).Returns((DateTime?)null);
        _mockAccountSettings.SetupGet(s => s.ImagesUntilDate).Returns((DateTime?)null);
        _mockAccountSettings.SetupGet(s => s.ImagesFromDays).Returns((int?)null);
        _mockAccountSettings.SetupGet(s => s.Rating).Returns((int?)null);
    }

    private List<AssetResponseDto> CreateSampleAssets()
    {
        return new List<AssetResponseDto>
        {
            new AssetResponseDto { Id = "1", Type = AssetTypeEnum.IMAGE, IsArchived = false, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddDays(-10), Rating = 5 } },
            new AssetResponseDto { Id = "2", Type = AssetTypeEnum.VIDEO, IsArchived = false, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddDays(-10) } }, // Should be filtered out by type
            new AssetResponseDto { Id = "3", Type = AssetTypeEnum.IMAGE, IsArchived = true, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddDays(-5), Rating = 3 } }, // Potentially filtered by archive status
            new AssetResponseDto { Id = "4", Type = AssetTypeEnum.IMAGE, IsArchived = false, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddDays(-2), Rating = 5 } },
            new AssetResponseDto { Id = "5", Type = AssetTypeEnum.IMAGE, IsArchived = false, ExifInfo = new ExifResponseDto { DateTimeOriginal = DateTime.Now.AddYears(-1), Rating = 1 } },
        };
    }

    [Test]
    public async Task GetAssetCount_ReturnsCorrectCount_AfterFiltering()
    {
        // Arrange
        var assets = CreateSampleAssets();
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(false); // Filter out archived

        // Act
        var count = await _testPool.GetAssetCount();

        // Assert
        // Expected: Asset "1", "4", "5" (Asset "2" is video, Asset "3" is archived)
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetAssets_ReturnsRequestedNumberOfAssets()
    {
        // Arrange
        var assets = CreateSampleAssets(); // Total 5 assets, 4 images if ShowArchived = true
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(true); // Asset "3" included

        // Act
        var result = (await _testPool.GetAssets(2)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        // All returned assets should be images
        Assert.That(result.All(a => a.Type == AssetTypeEnum.IMAGE));
    }

    [Test]
    public async Task GetAssets_ReturnsAllAvailableIfLessThanRequested()
    {
        // Arrange
        var assets = CreateSampleAssets().Where(a => a.Type == AssetTypeEnum.IMAGE && !a.IsArchived).ToList(); // 3 assets
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(false);

        // Act
        var result = (await _testPool.GetAssets(5)).ToList(); // Request 5, but only 3 available after filtering

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
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
        IEnumerable<AssetResponseDto>? cachedValue = null;
        _mockApiCache.Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<AssetResponseDto>>>>()
            ))
            .Returns<string, Func<Task<IEnumerable<AssetResponseDto>>>>(async (key, factory) =>
            {
                if (cachedValue == null)
                {
                    cachedValue = await factory();
                }

                return cachedValue;
            });

        // Act
        await _testPool.GetAssetCount(); // First call, should trigger LoadAssets
        await _testPool.GetAssetCount(); // Second call, should use cache
        await _testPool.GetAssets(1); // Third call, should use cache

        // Assert
        Assert.That(loadAssetsCallCount, Is.EqualTo(1), "LoadAssets should only be called once.");
    }

    [Test]
    public async Task ApplyAccountFilters_FiltersArchived()
    {
        // Arrange
        var assets = CreateSampleAssets(); // Asset "3" is archived
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(false);

        // Act
        var result = (await _testPool.GetAssets(5)).ToList(); // Request more than available to get all filtered

        // Assert
        Assert.That(result.Any(a => a.Id == "3"), Is.False);
        Assert.That(result.Count, Is.EqualTo(3)); // 1, 4, 5
    }

    [Test]
    public async Task ApplyAccountFilters_FiltersImagesUntilDate()
    {
        // Arrange
        var assets = CreateSampleAssets();
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        var untilDate = DateTime.Now.AddDays(-7); // Assets "1" (10 days ago), "5" (1 year ago) should match
        _mockAccountSettings.SetupGet(s => s.ImagesUntilDate).Returns(untilDate);
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(true); // Include asset "3" for date check if not filtered by archive

        // Act
        var result = (await _testPool.GetAssets(5)).ToList();

        // Assert (all are images already by default)
        // Assets: 1 (10d), 3 (5d, archived), 4 (2d), 5 (1y)
        // Filter: ShowArchived=true. UntilDate = -7d.
        // Expected: Asset "1", "5"
        Assert.That(result.All(a => a.ExifInfo.DateTimeOriginal <= untilDate));
        Assert.That(result.Count, Is.EqualTo(2), string.Join(",", result.Select(x => x.Id)));
        Assert.That(result.Any(a => a.Id == "1"));
        Assert.That(result.Any(a => a.Id == "5"));
    }

    [Test]
    public async Task ApplyAccountFilters_FiltersImagesFromDate()
    {
        // Arrange
        var assets = CreateSampleAssets();
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        var fromDate = DateTime.Now.AddDays(-7); // Assets "3" (5 days ago), "4" (2 days ago) should match
        _mockAccountSettings.SetupGet(s => s.ImagesFromDate).Returns(fromDate);
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(true);

        // Act
        var result = (await _testPool.GetAssets(5)).ToList();

        // Assert
        // Assets: 1 (10d), 3 (5d, archived), 4 (2d), 5 (1y)
        // Filter: ShowArchived=true. FromDate = -7d.
        // Expected: Asset "3", "4"
        Assert.That(result.All(a => a.ExifInfo.DateTimeOriginal >= fromDate));
        Assert.That(result.Count, Is.EqualTo(2), string.Join(",", result.Select(x => x.Id)));
        Assert.That(result.Any(a => a.Id == "3"));
        Assert.That(result.Any(a => a.Id == "4"));
    }

    [Test]
    public async Task ApplyAccountFilters_FiltersImagesFromDays()
    {
        // Arrange
        var assets = CreateSampleAssets();
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        _mockAccountSettings.SetupGet(s => s.ImagesFromDays).Returns(7); // Last 7 days
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(true);
        var fromDate = DateTime.Today.AddDays(-7);


        // Act
        var result = (await _testPool.GetAssets(5)).ToList();

        // Assert
        // Assets: 1 (10d), 3 (5d, archived), 4 (2d), 5 (1y)
        // Filter: ShowArchived=true. FromDays = 7 (so fromDate approx -7d from today).
        // Expected: Asset "3", "4"
        Assert.That(result.All(a => a.ExifInfo.DateTimeOriginal >= fromDate));
        Assert.That(result.Count, Is.EqualTo(2), string.Join(",", result.Select(x => x.Id)));
        Assert.That(result.Any(a => a.Id == "3"));
        Assert.That(result.Any(a => a.Id == "4"));
    }

    [Test]
    public async Task ApplyAccountFilters_FiltersRating()
    {
        // Arrange
        var assets = CreateSampleAssets(); // Asset "1" (rating 5), "3" (rating 3), "4" (rating 5), "5" (rating 1)
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);
        _mockAccountSettings.SetupGet(s => s.Rating).Returns(5);
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(true);


        // Act
        var result = (await _testPool.GetAssets(5)).ToList();

        // Assert
        // Expected: Asset "1", "4" (both rating 5)
        Assert.That(result.All(a => a.ExifInfo.Rating == 5));
        Assert.That(result.Count, Is.EqualTo(2), string.Join(",", result.Select(x => x.Id)));
        Assert.That(result.Any(a => a.Id == "1"));
        Assert.That(result.Any(a => a.Id == "4"));
    }

    [Test]
    public async Task ApplyAccountFilters_CombinedFilters()
    {
        // Arrange
        var assets = CreateSampleAssets();
        _testPool.LoadAssetsFunc = () => Task.FromResult<IEnumerable<AssetResponseDto>>(assets);

        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(false); // No archived (Asset "3" out)
        _mockAccountSettings.SetupGet(s => s.ImagesFromDays).Returns(15); // Last 15 days (Asset "5" out)
        // Assets "1" (10d), "4" (2d) remain
        _mockAccountSettings.SetupGet(s => s.Rating).Returns(5); // Asset "1" (rating 5), Asset "4" (rating 5)

        // Act
        var result = (await _testPool.GetAssets(5)).ToList();

        // Assert
        // Expected: Assets "1", "4"
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(a => a.Id == "1"));
        Assert.That(result.Any(a => a.Id == "4"));
        Assert.That(result.Any(a => a.Id == "3" || a.Id == "5" || a.Id == "2"), Is.False);
    }
}