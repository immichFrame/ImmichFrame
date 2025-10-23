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
public class AllAssetsPoolTests
{
    private Mock<IApiCache> _mockApiCache;
    private Mock<ImmichApi> _mockImmichApi;
    private Mock<IAccountSettings> _mockAccountSettings;
    private AllAssetsPool _allAssetsPool;

    [SetUp]
    public void Setup()
    {
        _mockApiCache = new Mock<IApiCache>();
        _mockImmichApi = new Mock<ImmichApi>(null!, null!);
        _mockAccountSettings = new Mock<IAccountSettings>();
        _allAssetsPool = new AllAssetsPool(_mockApiCache.Object, _mockImmichApi.Object, _mockAccountSettings.Object);

        // Default account settings
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(false);
        _mockAccountSettings.SetupGet(s => s.ImagesFromDate).Returns((DateTime?)null);
        _mockAccountSettings.SetupGet(s => s.ImagesUntilDate).Returns((DateTime?)null);
        _mockAccountSettings.SetupGet(s => s.ImagesFromDays).Returns((int?)null);
        _mockAccountSettings.SetupGet(s => s.Rating).Returns((int?)null);
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid>());

        // Default ApiCache setup
        _mockApiCache.Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<AssetStatsResponseDto>>>() // For GetAssetCount
            ))
            .Returns<string, Func<Task<AssetStatsResponseDto>>>(async (key, factory) => await factory());
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
        var stats = new AssetStatsResponseDto { Images = 100 };
        _mockImmichApi.Setup(api => api.GetAssetStatisticsAsync(null, false, null, It.IsAny<CancellationToken>())).ReturnsAsync(stats);

        // Act
        var count = await _allAssetsPool.GetAssetCount();

        // Assert
        Assert.That(count, Is.EqualTo(100));
        _mockImmichApi.Verify(api => api.GetAssetStatisticsAsync(null, false, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockApiCache.Verify(cache => cache.GetOrAddAsync(nameof(AllAssetsPool), It.IsAny<Func<Task<AssetStatsResponseDto>>>()), Times.Once);
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
