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
public class FavoriteAssetsPoolTests
{
    private Mock<IApiCache> _mockApiCache;
    private Mock<ImmichApi> _mockImmichApi;
    private Mock<IAccountSettings> _mockAccountSettings; // Though not directly used by LoadAssets here
    private Mock<IGeneralSettings> _mockGeneralSettings;
    private TestableFavoriteAssetsPool _favoriteAssetsPool;

    private class TestableFavoriteAssetsPool : FavoriteAssetsPool
    {
        public TestableFavoriteAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings, IGeneralSettings generalSettings)
            : base(apiCache, immichApi, accountSettings, generalSettings) { }

        public Task<IEnumerable<AssetResponseDto>> TestLoadAssets(CancellationToken ct = default)
        {
            return base.LoadAssets(ct);
        }
    }

    [SetUp]
    public void Setup()
    {
        _mockApiCache = new Mock<IApiCache>();
        _mockImmichApi = new Mock<ImmichApi>("", null!);
        _mockAccountSettings = new Mock<IAccountSettings>();
        _mockGeneralSettings = new Mock<IGeneralSettings>();
        _favoriteAssetsPool = new TestableFavoriteAssetsPool(_mockApiCache.Object, _mockImmichApi.Object, _mockAccountSettings.Object, _mockGeneralSettings.Object);
    }

    private AssetResponseDto CreateAsset(string id) => new AssetResponseDto { Id = id, Type = AssetTypeEnum.IMAGE };
    private SearchResponseDto CreateSearchResult(List<AssetResponseDto> assets, int total) =>
        new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = assets, Total = total } };

    [Test]
    public async Task LoadAssets_CallsSearchAssetsAsync_WithFavoriteTrue_AndPaginates()
    {
        // Arrange
        var batchSize = 1000; // From FavoriteAssetsPool.cs
        var assetsPage1 = Enumerable.Range(0, batchSize).Select(i => CreateAsset($"fav_p1_{i}")).ToList();
        var assetsPage2 = Enumerable.Range(0, 50).Select(i => CreateAsset($"fav_p2_{i}")).ToList();

        _mockImmichApi.SetupSequence(api => api.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(assetsPage1, batchSize)) // Page 1, total indicates more might be available
            .ReturnsAsync(CreateSearchResult(assetsPage2, 50));      // Page 2, total indicates this is the last page

        // Act
        var result = (await _favoriteAssetsPool.TestLoadAssets()).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(batchSize + 50));
        Assert.That(result.Any(a => a.Id == "fav_p1_0"));
        Assert.That(result.Any(a => a.Id == "fav_p2_49"));

        _mockImmichApi.Verify(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto =>
                dto.IsFavorite == true &&
                dto.Type == AssetTypeEnum.IMAGE &&
                dto.WithExif == true &&
                dto.WithPeople == true &&
                dto.Page == 1 && dto.Size == batchSize),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockImmichApi.Verify(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto =>
                dto.IsFavorite == true &&
                dto.Type == AssetTypeEnum.IMAGE &&
                dto.Page == 2 && dto.Size == batchSize),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_HandlesEmptyFavorites()
    {
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto>(), 0));

        var result = (await _favoriteAssetsPool.TestLoadAssets()).ToList();
        Assert.That(result, Is.Empty);
    }
}
