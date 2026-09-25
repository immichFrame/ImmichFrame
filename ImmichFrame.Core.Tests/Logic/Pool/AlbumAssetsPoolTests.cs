using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class AlbumAssetsPoolTests
{
    private Mock<IApiCache> _mockApiCache;
    private Mock<ImmichApi> _mockImmichApi;
    private Mock<IAccountSettings> _mockAccountSettings;
    private AlbumAssetsPool _albumAssetsPool;

    [SetUp]
    public void Setup()
    {
        _mockApiCache = new Mock<IApiCache>();

        _mockApiCache
            .Setup(m => m.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<AssetResponseDto>>>>()))
            .Returns<string, Func<Task<IEnumerable<AssetResponseDto>>>>((_, factory) => factory());

        _mockImmichApi = new Mock<ImmichApi>("", null);
        _mockAccountSettings = new Mock<IAccountSettings>();
        _albumAssetsPool = new AlbumAssetsPool(_mockApiCache.Object, _mockImmichApi.Object, _mockAccountSettings.Object);

        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid>());
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid>());
    }

    private AssetResponseDto CreateAsset(string id) => new AssetResponseDto { Id = FixtureHelpers.GuidFor(id), Type = AssetTypeEnum.IMAGE };

    [Test]
    public async Task LoadAssets_ReturnsAssetsPresentIIncludedNotExcludedAlbums()
    {
        // Arrange
        var album1Id = Guid.NewGuid();
        var excludedAlbumId = Guid.NewGuid();

        var assetA = CreateAsset("A"); // In album1
        var assetB = CreateAsset("B"); // In album1 and excludedAlbum
        var assetC = CreateAsset("C"); // In excludedAlbum only
        var assetD = CreateAsset("D"); // In album1 only

        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid> { album1Id });
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid> { excludedAlbumId });

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(album1Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { assetA, assetB, assetD }, Total = 3 } });
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(excludedAlbumId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { assetB, assetC }, Total = 2 } });

        // Act
        var result = (await _albumAssetsPool.GetAssets(25)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(a => a.Id == FixtureHelpers.GuidFor("A")));
        Assert.That(result.Any(a => a.Id == FixtureHelpers.GuidFor("D")));
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(album1Id)), It.IsAny<CancellationToken>()), Times.Once);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(excludedAlbumId)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_HideAssetsInOtherAlbums_HidesAssetsAlsoInUnselectedAlbums()
    {
        // Arrange
        var selectedAlbumId = Guid.NewGuid();
        var otherAlbumId = Guid.NewGuid();
        var sharedAlbumId = Guid.NewGuid();

        var assetA = CreateAsset("A"); // In selected album only
        var assetB = CreateAsset("B"); // In selected album and other album
        var assetC = CreateAsset("C"); // In selected album and an album shared with the user
        var assetD = CreateAsset("D"); // In selected album only

        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid> { selectedAlbumId });
        _mockAccountSettings.SetupGet(s => s.HideAssetsInOtherAlbums).Returns(true);

        _mockImmichApi.Setup(api => api.GetAllAlbumsAsync(null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlbumResponseDto>
            {
                new AlbumResponseDto { Id = selectedAlbumId },
                new AlbumResponseDto { Id = otherAlbumId },
                new AlbumResponseDto { Id = sharedAlbumId, Shared = true }
            });
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(selectedAlbumId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { assetA, assetB, assetC, assetD }, Total = 4 } });
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(otherAlbumId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { assetB, CreateAsset("E") }, Total = 2 } });
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(sharedAlbumId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { assetC }, Total = 1 } });

        // Act
        var result = (await _albumAssetsPool.GetAssets(25)).ToList();

        // Assert
        Assert.That(result.Select(a => a.Id), Is.EquivalentTo(new[] { assetA.Id, assetD.Id }));
        _mockImmichApi.Verify(api => api.GetAllAlbumsAsync(null, null, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
        // The selected album is loaded for display but never fetched as an exclusion
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(selectedAlbumId)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_HideAssetsInOtherAlbums_CombinesWithExcludedAlbums()
    {
        var selectedAlbumId = Guid.NewGuid();
        var otherAlbumId = Guid.NewGuid();

        var assetA = CreateAsset("A");
        var assetB = CreateAsset("B"); // Also in the other album, which is listed as excluded too

        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid> { selectedAlbumId });
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid> { otherAlbumId });
        _mockAccountSettings.SetupGet(s => s.HideAssetsInOtherAlbums).Returns(true);

        _mockImmichApi.Setup(api => api.GetAllAlbumsAsync(null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlbumResponseDto> { new AlbumResponseDto { Id = selectedAlbumId }, new AlbumResponseDto { Id = otherAlbumId } });
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(selectedAlbumId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { assetA, assetB }, Total = 2 } });
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(otherAlbumId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { assetB }, Total = 1 } });

        var result = (await _albumAssetsPool.GetAssets(25)).ToList();

        Assert.That(result.Select(a => a.Id), Is.EquivalentTo(new[] { assetA.Id }));
        // An album that is both excluded and "other" is only fetched once
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(otherAlbumId)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_HideAssetsInOtherAlbumsDisabled_DoesNotListAlbums()
    {
        var selectedAlbumId = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid> { selectedAlbumId });
        _mockAccountSettings.SetupGet(s => s.HideAssetsInOtherAlbums).Returns(false);

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(selectedAlbumId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { CreateAsset("A") }, Total = 1 } });

        var result = (await _albumAssetsPool.GetAssets(25)).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        _mockImmichApi.Verify(api => api.GetAllAlbumsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task LoadAssets_NoIncludedAlbums_ReturnsEmpty()
    {
        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid>());
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid> { Guid.NewGuid() });
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { CreateAsset("excluded_only") }, Total = 1 } });


        var result = (await _albumAssetsPool.GetAssets(25)).ToList();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task LoadAssets_NoExcludedAlbums_ReturnsAlbums()
    {
        var album1Id = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid> { album1Id });
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid>()); // Empty excluded

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<MetadataSearchDto>(d => d.AlbumIds.Contains(album1Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = new List<AssetResponseDto> { CreateAsset("A") }, Total = 1 } });

        var result = (await _albumAssetsPool.GetAssets(25)).ToList();
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.Any(a => a.Id == FixtureHelpers.GuidFor("A")));
    }

    [Test]
    public async Task LoadAssets_NullAlbums_ReturnsEmpty()
    {
        _mockAccountSettings.SetupGet(s => s.Albums).Returns((List<Guid>)null);

        var result = (await _albumAssetsPool.GetAssets(25)).ToList();
        Assert.That(result, Is.Empty);

        // the absence of an error, whereas before a null pointer exception would be thrown, indicates success.
    }

    [Test]
    public async Task LoadAssets_NullExcludedAlbums_Succeeds()
    {
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns((List<Guid>)null);

        var result = (await _albumAssetsPool.GetAssets(25)).ToList();
        Assert.That(result, Is.Empty);

        // the absence of an error, whereas before a null pointer exception would be thrown, indicates success.
    }
}
