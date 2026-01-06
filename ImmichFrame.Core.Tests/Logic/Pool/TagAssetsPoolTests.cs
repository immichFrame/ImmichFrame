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
public class TagAssetsPoolTests
{
    private Mock<IApiCache> _mockApiCache;
    private Mock<ImmichApi> _mockImmichApi;
    private Mock<IAccountSettings> _mockAccountSettings;
    private TestableTagAssetsPool _tagAssetsPool;

    private class TestableTagAssetsPool : TagAssetsPool
    {
        public TestableTagAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings)
            : base(apiCache, immichApi, accountSettings) { }

        public Task<IEnumerable<AssetResponseDto>> TestLoadAssets(CancellationToken ct = default)
        {
            return base.LoadAssets(ct);
        }
    }

    [SetUp]
    public void Setup()
    {
        _mockApiCache = new Mock<IApiCache>();
        _mockImmichApi = new Mock<ImmichApi>(null, null);
        _mockAccountSettings = new Mock<IAccountSettings>();
        _tagAssetsPool = new TestableTagAssetsPool(_mockApiCache.Object, _mockImmichApi.Object, _mockAccountSettings.Object);

        _mockAccountSettings.SetupGet(s => s.Tags).Returns(new List<string>());
    }

    private AssetResponseDto CreateAsset(string id) => new AssetResponseDto { Id = id, Type = AssetTypeEnum.IMAGE, Tags = new List<TagResponseDto>() };
    private SearchResponseDto CreateSearchResult(List<AssetResponseDto> assets, int total) =>
        new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = assets, Total = total } };

    [Test]
    public async Task LoadAssets_CallsSearchAssetsForEachTag_AndPaginates()
    {
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.Tags).Returns(new List<string> { "Vacation", "Family" });

        var allTags = new List<TagResponseDto>
        {
            new TagResponseDto { Id = tag1Id.ToString(), Name = "Vacation", Value = "Vacation" },
            new TagResponseDto { Id = tag2Id.ToString(), Name = "Family", Value = "Family" }
        };
        _mockImmichApi.Setup(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allTags);

        var batchSize = 1000;
        var t1AssetsPage1 = Enumerable.Range(0, batchSize).Select(i => CreateAsset($"t1_p1_{i}")).ToList();
        var t1AssetsPage2 = Enumerable.Range(0, 30).Select(i => CreateAsset($"t1_p2_{i}")).ToList();
        var t2AssetsPage1 = Enumerable.Range(0, 20).Select(i => CreateAsset($"t2_p1_{i}")).ToList();

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id) && d.Page == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(t1AssetsPage1, batchSize));
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id) && d.Page == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(t1AssetsPage2, 30));
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2Id) && d.Page == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(t2AssetsPage1, 20));

        var result = (await _tagAssetsPool.TestLoadAssets()).ToList();

        Assert.That(result.Count, Is.EqualTo(batchSize + 30 + 20));
        Assert.That(result.Any(a => a.Id == "t1_p1_0"));
        Assert.That(result.Any(a => a.Id == "t1_p2_29"));
        Assert.That(result.Any(a => a.Id == "t2_p1_19"));

        _mockImmichApi.Verify(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id) && d.Page == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id) && d.Page == 2), It.IsAny<CancellationToken>()), Times.Once);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2Id) && d.Page == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_NoTagsConfigured_ReturnsEmpty()
    {
        _mockAccountSettings.SetupGet(s => s.Tags).Returns(new List<string>());

        _mockImmichApi.Setup(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagResponseDto>());

        var result = (await _tagAssetsPool.TestLoadAssets()).ToList();
        Assert.That(result, Is.Empty);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task LoadAssets_TagHasNoAssets_DoesNotAffectOthers()
    {
        var tag1Id = Guid.NewGuid(); // Has assets
        var tag2Id = Guid.NewGuid(); // No assets
        _mockAccountSettings.SetupGet(s => s.Tags).Returns(new List<string> { "Vacation", "Work" });

        var allTags = new List<TagResponseDto>
        {
            new TagResponseDto { Id = tag1Id.ToString(), Name = "Vacation", Value = "Vacation" },
            new TagResponseDto { Id = tag2Id.ToString(), Name = "Work", Value = "Work" }
        };
        _mockImmichApi.Setup(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allTags);

        var t1Assets = Enumerable.Range(0, 10).Select(i => CreateAsset($"t1_{i}")).ToList();
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(t1Assets, 10));
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto>(), 0));

        var result = (await _tagAssetsPool.TestLoadAssets()).ToList();
        Assert.That(result.Count, Is.EqualTo(10));
        Assert.That(result.All(a => a.Id.StartsWith("t1_")));
    }

    [Test]
    public async Task LoadAssets_PassesCorrectMetadataSearchParameters()
    {
        var tagId = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.Tags).Returns(new List<string> { "Vacation" });

        var allTags = new List<TagResponseDto>
        {
            new TagResponseDto { Id = tagId.ToString(), Name = "Vacation", Value = "Vacation" }
        };
        _mockImmichApi.Setup(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allTags);

        var assets = Enumerable.Range(0, 5).Select(i => CreateAsset($"asset_{i}")).ToList();
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(assets, 5));

        await _tagAssetsPool.TestLoadAssets();

        _mockImmichApi.Verify(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(d =>
                d.TagIds.Contains(tagId) &&
                d.Page == 1 &&
                d.Size == 1000 &&
                d.Type == AssetTypeEnum.IMAGE &&
                d.WithExif == true &&
                d.WithPeople == true
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_EnrichesAssetsWithNullTags_CallsGetAssetInfoAsync()
    {
        var tagId = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.Tags).Returns(new List<string> { "TestTag" });

        var allTags = new List<TagResponseDto>
        {
            new TagResponseDto { Id = tagId.ToString(), Name = "TestTag", Value = "TestTag" }
        };
        _mockImmichApi.Setup(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allTags);

        var assetId = Guid.NewGuid();
        var assetWithNullTags = new AssetResponseDto
        {
            Id = assetId.ToString(),
            Type = AssetTypeEnum.IMAGE,
            Tags = null  // Key: Tags is null, should trigger GetAssetInfoAsync
        };

        var enrichedAsset = new AssetResponseDto
        {
            Id = assetId.ToString(),
            Type = AssetTypeEnum.IMAGE,
            Tags = new List<TagResponseDto> { new TagResponseDto { Id = tagId.ToString(), Name = "TestTag" } },
            ExifInfo = new ExifResponseDto { Make = "TestCamera" },
            People = new List<PersonWithFacesResponseDto> { new PersonWithFacesResponseDto { Name = "TestPerson" } }
        };

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tagId)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { assetWithNullTags }, 1));

        _mockImmichApi.Setup(api => api.GetAssetInfoAsync(
            assetId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrichedAsset);

        var result = (await _tagAssetsPool.TestLoadAssets()).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Tags, Is.Not.Null);
        Assert.That(result[0].ExifInfo, Is.Not.Null);
        Assert.That(result[0].People, Is.Not.Null);
        Assert.That(result[0].Tags!.Count, Is.EqualTo(1));
        Assert.That(result[0].Tags!.First().Name, Is.EqualTo("TestTag"));
        _mockImmichApi.Verify(api => api.GetAssetInfoAsync(
            assetId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssets_HierarchicalTags_MatchesByFullValue()
    {
        // Arrange - Three tags with the same name "Child" but different full paths
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        var tag3Id = Guid.NewGuid();
        // User configures the full hierarchical path
        _mockAccountSettings.SetupGet(s => s.Tags).Returns(new List<string> { "Parent1/Child" });

        // Mock GetAllTagsAsync to return three tags with same name but different values
        var allTags = new List<TagResponseDto>
        {
            new TagResponseDto { Id = tag1Id.ToString(), Name = "Child", Value = "Parent1/Child" },
            new TagResponseDto { Id = tag2Id.ToString(), Name = "Child", Value = "Parent2/Child" },
            new TagResponseDto { Id = tag3Id.ToString(), Name = "Child", Value = "Child" }
        };
        _mockImmichApi.Setup(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allTags);

        var tag1Assets = Enumerable.Range(0, 5).Select(i => CreateAsset($"tag1_{i}")).ToList();

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(tag1Assets, 5));

        var result = (await _tagAssetsPool.TestLoadAssets()).ToList();

        // Assert - Should only include assets from the exact matching tag value
        Assert.That(result.Count, Is.EqualTo(5));
        Assert.That(result.All(a => a.Id.StartsWith("tag1_")));

        _mockImmichApi.Verify(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Should only search for the one matching tag, not the others
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id)), It.IsAny<CancellationToken>()), Times.Once);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2Id)), It.IsAny<CancellationToken>()), Times.Never);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag3Id)), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task LoadAssets_AssetWithMultipleTags_OnlyIncludedOnce()
    {
        // Arrange - Configure two tags that an asset has both of
        var tag1Id = Guid.NewGuid();
        var tag2Id = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.Tags).Returns(new List<string> { "Vacation", "Family" });

        var allTags = new List<TagResponseDto>
        {
            new TagResponseDto { Id = tag1Id.ToString(), Name = "Vacation", Value = "Vacation" },
            new TagResponseDto { Id = tag2Id.ToString(), Name = "Family", Value = "Family" }
        };
        _mockImmichApi.Setup(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allTags);

        // Same asset returned for both tags
        var sharedAssetId = Guid.NewGuid().ToString();
        var sharedAsset = CreateAsset(sharedAssetId);
        var tag1OnlyAsset = CreateAsset("tag1_only");
        var tag2OnlyAsset = CreateAsset("tag2_only");

        var tag1Assets = new List<AssetResponseDto> { sharedAsset, tag1OnlyAsset };
        var tag2Assets = new List<AssetResponseDto> { sharedAsset, tag2OnlyAsset };

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(tag1Assets, 2));
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(tag2Assets, 2));

        // Act
        var result = (await _tagAssetsPool.TestLoadAssets()).ToList();

        // Assert - Should only include the shared asset once, plus the two unique assets
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Count(a => a.Id == sharedAssetId), Is.EqualTo(1), "Shared asset should only appear once");
        Assert.That(result.Any(a => a.Id == "tag1_only"), "Should include tag1-only asset");
        Assert.That(result.Any(a => a.Id == "tag2_only"), "Should include tag2-only asset");

        _mockImmichApi.Verify(api => api.GetAllTagsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1Id)), It.IsAny<CancellationToken>()), Times.Once);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2Id)), It.IsAny<CancellationToken>()), Times.Once);
    }
}
