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

    private AssetResponseDto CreateAsset(string id) => new AssetResponseDto { Id = id, Type = AssetTypeEnum.IMAGE };

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

        _mockImmichApi.Setup(api => api.GetAlbumInfoAsync(album1Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlbumResponseDto { Assets = new List<AssetResponseDto> { assetA, assetB, assetD } });
        _mockImmichApi.Setup(api => api.GetAlbumInfoAsync(excludedAlbumId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlbumResponseDto { Assets = new List<AssetResponseDto> { assetB, assetC } });

        // Act
        var result = (await _albumAssetsPool.GetAssets(25)).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(a => a.Id == "A"));
        Assert.That(result.Any(a => a.Id == "D"));
        _mockImmichApi.Verify(api => api.GetAlbumInfoAsync(album1Id, null, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockImmichApi.Verify(api => api.GetAlbumInfoAsync(excludedAlbumId, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_NoIncludedAlbums_ReturnsEmpty()
    {
        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid>());
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid> { Guid.NewGuid() });
        _mockImmichApi.Setup(api => api.GetAlbumInfoAsync(It.IsAny<Guid>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlbumResponseDto { Assets = new List<AssetResponseDto> { CreateAsset("excluded_only") } });


        var result = (await _albumAssetsPool.GetAssets(25)).ToList();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task LoadAssets_NoExcludedAlbums_ReturnsAlbums()
    {
        var album1Id = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid> { album1Id });
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid>()); // Empty excluded

        _mockImmichApi.Setup(api => api.GetAlbumInfoAsync(album1Id, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlbumResponseDto { Assets = new List<AssetResponseDto> { CreateAsset("A") } });

        var result = (await _albumAssetsPool.GetAssets(25)).ToList();
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.Any(a => a.Id == "A"));
    }

    [Test]
    public async Task LoadAssets_NullAlbums_ReturnsEmpty()
    {
        _mockAccountSettings.SetupGet(s => s.Albums).Returns((List<Guid>)null);

        var result = (await _albumAssetsPool.TestLoadAssets()).ToList();
        Assert.That(result, Is.Empty);

        // the absence of an error, whereas before a null pointer exception would be thrown, indicates success.
    }

    [Test]
    public async Task LoadAssets_NullExcludedAlbums_Succeeds()
    {
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns((List<Guid>)null);

        var result = (await _albumAssetsPool.TestLoadAssets()).ToList();
        Assert.That(result, Is.Empty);

        // the absence of an error, whereas before a null pointer exception would be thrown, indicates success.
    }
}
