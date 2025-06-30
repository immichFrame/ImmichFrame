using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool.Remote;
using System.Net.Http; // Added for HttpClient

namespace ImmichFrame.Core.Tests.Logic.Pool.Remote;

[TestFixture]
public class AllAssetsRemotePoolTests
{
    private FixtureHelpers.ForgetfulCountingCache _fakeCache;
    private Mock<ImmichApi> _mockImmichApi;
    private Mock<IAccountSettings> _mockAccountSettings;
    private AllAssetsRemotePool _allAssetsPool;

    [SetUp]
    public void Setup()
    {
        _fakeCache = new FixtureHelpers.ForgetfulCountingCache();
        _mockImmichApi = new Mock<ImmichApi>("http://dummy-url.com", new HttpClient());
        _mockAccountSettings = new Mock<IAccountSettings>();
        _allAssetsPool = new AllAssetsRemotePool(_fakeCache, _mockImmichApi.Object, _mockAccountSettings.Object, FixtureHelpers.TestLogger<AllAssetsRemotePool>());

        // Default account settings
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(false);
        _mockAccountSettings.SetupGet(s => s.ImagesFromDate).Returns((DateTime?)null);
        _mockAccountSettings.SetupGet(s => s.ImagesUntilDate).Returns((DateTime?)null);
        _mockAccountSettings.SetupGet(s => s.ImagesFromDays).Returns((int?)null);
        _mockAccountSettings.SetupGet(s => s.Rating).Returns((int?)null);
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid>());
    }

    private List<AssetResponseDto> CreateSampleAssets(int count, string idPrefix = "asset")
    {
        return Enumerable.Range(0, count)
            .Select(i => new AssetResponseDto { Id = $"{idPrefix}{i}", Type = AssetTypeEnum.IMAGE })
            .ToList();
    }
    
    [Test]
    public async Task GetAssetCount_CallsApiAndCache()
    {
        // Arrange
        var stats = new SearchStatisticsResponseDto { Total = 100 };
        _mockImmichApi.Setup(api => api.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(stats);
        
        // Act
        var count = await _allAssetsPool.GetAssetCount();

        // Assert
        Assert.That(count, Is.EqualTo(100));
        _mockImmichApi.Verify(api => api.SearchAssetStatisticsAsync(
            It.Is<StatisticsSearchDto>(dto => dto.Type == AssetTypeEnum.IMAGE),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(_fakeCache.Count, Is.EqualTo(1));
    }
    

    [Test]
    public async Task GetAssetCount_CallsApiAndFallsBack()
    {
        // Arrange
        // Setup the primary call to throw an exception, forcing the fallback
        _mockImmichApi.Setup(api => api.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated API failure for primary call"));

        var fallbackStats = new AssetStatsResponseDto { Images = 100 };
        _mockImmichApi.Setup(api => api.GetAssetStatisticsAsync(null, false, null, It.IsAny<CancellationToken>())).ReturnsAsync(fallbackStats);
        
        // Act
        var count = await _allAssetsPool.GetAssetCount();

        // Assert
        Assert.That(count, Is.EqualTo(100));
        _mockImmichApi.Verify(api => api.GetAssetStatisticsAsync(null, false, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(_fakeCache.Count, Is.EqualTo(2), "Cache count should be 2: one for the failed primary attempt, one for the successful fallback.");
    }

    [Test]
    public async Task GetAssets_CallsSearchRandomAsync_WithCorrectParameters()
    {
        // Arrange
        var requestedCount = 5;
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(true);
        _mockAccountSettings.SetupGet(s => s.Rating).Returns(3);
        var returnedAssets = CreateSampleAssets(requestedCount);
        _mockImmichApi.Setup(api => api.SearchRandomAsync(It.IsAny<RandomSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedAssets);

        // Act
        var assets = await _allAssetsPool.GetAssets(requestedCount);

        // Assert
        Assert.That(assets.Count(), Is.EqualTo(requestedCount));
        _mockImmichApi.Verify(api => api.SearchRandomAsync(
            It.Is<RandomSearchDto>(dto =>
                dto.Size == requestedCount &&
                dto.Type == AssetTypeEnum.IMAGE &&
                dto.WithExif == true &&
                dto.WithPeople == true &&
                dto.Visibility == AssetVisibility.Archive && // ShowArchived = true
                dto.Rating == 3
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAssets_AppliesDateFilters_FromDays()
    {
        _mockAccountSettings.SetupGet(s => s.ImagesFromDays).Returns(10);
        var expectedFromDate = DateTime.Today.AddDays(-10);
         _mockImmichApi.Setup(api => api.SearchRandomAsync(It.IsAny<RandomSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetResponseDto>());

        await _allAssetsPool.GetAssets(5);

        _mockImmichApi.Verify(api => api.SearchRandomAsync(
            It.Is<RandomSearchDto>(dto => dto.TakenAfter.HasValue && dto.TakenAfter.Value.Date == expectedFromDate.Date),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAssets_ExcludesAssetsFromExcludedAlbums()
    {
        // Arrange
        var mainAssets = CreateSampleAssets(3, "main"); // main0, main1, main2
        var excludedAsset = new AssetResponseDto { Id = "excluded1", Type = AssetTypeEnum.IMAGE };
        var assetsToReturnFromSearch = new List<AssetResponseDto>(mainAssets) { excludedAsset };

        var excludedAlbumId = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid> { excludedAlbumId });

        _mockImmichApi.Setup(api => api.SearchRandomAsync(It.IsAny<RandomSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assetsToReturnFromSearch);
        _mockImmichApi.Setup(api => api.GetAlbumInfoAsync(excludedAlbumId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlbumResponseDto { Assets = new List<AssetResponseDto> { excludedAsset }, AssetCount = 1 });

        // Act
        var result = (await _allAssetsPool.GetAssets(4)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Any(a => a.Id == "excluded1"), Is.False);
        Assert.That(result.All(a => a.Id.StartsWith("main")));
        _mockImmichApi.Verify(api => api.GetAlbumInfoAsync(excludedAlbumId, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
