using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class TagAssetsPoolTests
{
    private Mock<IApiCache> _cache;
    private Mock<ImmichApi> _api;
    private Mock<IAccountSettings> _settings;
    private TestableTagAssetsPool _pool;

    private class TestableTagAssetsPool(IApiCache cache, ImmichApi api, IAccountSettings settings) : TagAssetsPool(cache, api, settings)
    {
        public Task<IEnumerable<AssetResponseDto>> LoadAssetsPublic(CancellationToken ct = default) => LoadAssets(ct);
    }

    [SetUp]
    public void Setup()
    {
        _cache = new Mock<IApiCache>();
        _cache.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<Func<Task<ICollection<TagResponseDto>>>>()))
            .Returns<string, Func<Task<ICollection<TagResponseDto>>>>((_, f) => f());

        _api = new Mock<ImmichApi>(null, null);
        _settings = new Mock<IAccountSettings>();
        _pool = new TestableTagAssetsPool(_cache.Object, _api.Object, _settings.Object);
    }

    private static AssetResponseDto Asset(string id) => new() { Id = id, Type = AssetTypeEnum.IMAGE };

    private static SearchResponseDto SearchResult(List<AssetResponseDto> assets) =>
        new() { Assets = new SearchAssetResponseDto { Items = assets, Total = assets.Count } };

    [Test]
    public async Task LoadAssets_PaginatesAndCombinesMultipleTags()
    {
        var tag1 = Guid.NewGuid();
        var tag2 = Guid.NewGuid();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "Tag1", "Tag2" });

        _api.Setup(a => a.GetAllTagsAsync(default))
            .ReturnsAsync([
                new TagResponseDto { Id = tag1.ToString(), Name = "Tag1", Value = "Tag1" },
                new TagResponseDto { Id = tag2.ToString(), Name = "Tag2", Value = "Tag2" }
            ]);

        var page1 = Enumerable.Range(0, 1000).Select(i => Asset($"t1_p1_{i}")).ToList();
        var page2 = Enumerable.Range(0, 30).Select(i => Asset($"t1_p2_{i}")).ToList();
        var tag2Assets = Enumerable.Range(0, 20).Select(i => Asset($"t2_{i}")).ToList();

        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1) && d.Page == 1), default))
            .ReturnsAsync(SearchResult(page1));
        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1) && d.Page == 2), default))
            .ReturnsAsync(SearchResult(page2));
        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2)), default))
            .ReturnsAsync(SearchResult(tag2Assets));

        var result = (await _pool.LoadAssetsPublic()).ToList();

        Assert.That(result, Has.Count.EqualTo(1050));
        Assert.Multiple(() =>
        {
            Assert.That(result.Any(a => a.Id == "t1_p1_0"));
            Assert.That(result.Any(a => a.Id == "t1_p2_29"));
            Assert.That(result.Any(a => a.Id == "t2_19"));
        });
    }

    [Test]
    public async Task LoadAssets_NullTagsConfigured_ReturnsEmpty()
    {
        _settings.SetupGet(s => s.Tags).Returns(null as List<string>);
        _api.Setup(a => a.GetAllTagsAsync(default)).ReturnsAsync([]);

        var result = await _pool.LoadAssetsPublic();

        Assert.That(result, Is.Empty);
        _api.Verify(a => a.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), default), Times.Never);
    }

    [Test]
    public async Task LoadAssets_NoTagsConfigured_ReturnsEmpty()
    {
        _settings.SetupGet(s => s.Tags).Returns(new List<string>());
        _api.Setup(a => a.GetAllTagsAsync(default)).ReturnsAsync([]);

        var result = await _pool.LoadAssetsPublic();

        Assert.That(result, Is.Empty);
        _api.Verify(a => a.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), default), Times.Never);
    }

    [Test]
    public async Task LoadAssets_EmptyTagResultsAreIgnored()
    {
        var tag1 = Guid.NewGuid();
        var tag2 = Guid.NewGuid();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "HasAssets", "Empty" });

        _api.Setup(a => a.GetAllTagsAsync(default))
            .ReturnsAsync([
                new TagResponseDto { Id = tag1.ToString(), Value = "HasAssets" },
                new TagResponseDto { Id = tag2.ToString(), Value = "Empty" }
            ]);

        var assets = Enumerable.Range(0, 10).Select(i => Asset($"asset_{i}")).ToList();
        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1)), default))
            .ReturnsAsync(SearchResult(assets));
        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2)), default))
            .ReturnsAsync(SearchResult([]));

        var result = (await _pool.LoadAssetsPublic()).ToList();

        Assert.That(result, Has.Count.EqualTo(10));
        Assert.That(result.All(a => a.Id.StartsWith("asset_")));
    }

    [Test]
    public async Task LoadAssets_PassesCorrectSearchParameters()
    {
        var tagId = Guid.NewGuid();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "Tag" });

        _api.Setup(a => a.GetAllTagsAsync(default))
            .ReturnsAsync([new TagResponseDto { Id = tagId.ToString(), Value = "Tag" }]);
        _api.Setup(a => a.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), default))
            .ReturnsAsync(SearchResult([Asset("1")]));

        await _pool.LoadAssetsPublic();

        _api.Verify(a => a.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(d =>
                d.TagIds.Contains(tagId) &&
                d.Page == 1 &&
                d.Size == 1000 &&
                d.Type == AssetTypeEnum.IMAGE &&
                d.WithExif == true &&
                d.WithPeople == true
            ), default), Times.Once);
    }

    [Test]
    public async Task LoadAssets_AttachesTagsDirectlyWithoutExtraApiCalls()
    {
        var tagId = Guid.NewGuid();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "Tag" });

        _api.Setup(a => a.GetAllTagsAsync(default))
            .ReturnsAsync([new TagResponseDto { Id = tagId.ToString(), Value = "Tag" }]);

        var assetWithoutTags = new AssetResponseDto
        {
            Id = "asset-1",
            Type = AssetTypeEnum.IMAGE,
            Tags = null,
            ExifInfo = new ExifResponseDto { Make = "Camera" },
            People = [new PersonWithFacesResponseDto { Name = "Person" }]
        };

        _api.Setup(a => a.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), default))
            .ReturnsAsync(SearchResult([assetWithoutTags]));

        var result = (await _pool.LoadAssetsPublic()).ToList();

        var asset = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(asset.Tags, Has.Count.EqualTo(1));
            Assert.That(asset.Tags!.First().Id, Is.EqualTo(tagId.ToString()));
            Assert.That(asset.ExifInfo, Is.Not.Null);
            Assert.That(asset.People, Is.Not.Null);
        });

        _api.Verify(a => a.GetAssetInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }

    [Test]
    public async Task LoadAssets_MatchesHierarchicalTagsByFullPath()
    {
        var match = Guid.NewGuid();
        var noMatch1 = Guid.NewGuid();
        var noMatch2 = Guid.NewGuid();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "Parent1/Child" });

        _api.Setup(a => a.GetAllTagsAsync(default))
            .ReturnsAsync([
                new TagResponseDto { Id = match.ToString(), Value = "Parent1/Child" },
                new TagResponseDto { Id = noMatch1.ToString(), Value = "Parent2/Child" },
                new TagResponseDto { Id = noMatch2.ToString(), Value = "Child" }
            ]);

        var assets = Enumerable.Range(0, 5).Select(i => Asset($"asset_{i}")).ToList();
        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(match)), default))
            .ReturnsAsync(SearchResult(assets));

        var result = (await _pool.LoadAssetsPublic()).ToList();

        Assert.That(result, Has.Count.EqualTo(5));
        _api.Verify(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(match)), default), Times.Once);
        _api.Verify(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(noMatch1)), default), Times.Never);
        _api.Verify(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(noMatch2)), default), Times.Never);
    }

    [Test]
    public async Task LoadAssets_MatchesTagsCaseSensitively()
    {
        var lower = Guid.NewGuid();
        var upper = Guid.NewGuid();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "people" });

        _api.Setup(a => a.GetAllTagsAsync(default))
            .ReturnsAsync([
                new TagResponseDto { Id = lower.ToString(), Value = "people" },
                new TagResponseDto { Id = upper.ToString(), Value = "People" }
            ]);

        var assets = Enumerable.Range(0, 5).Select(i => Asset($"asset_{i}")).ToList();
        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(lower)), default))
            .ReturnsAsync(SearchResult(assets));

        var result = (await _pool.LoadAssetsPublic()).ToList();

        Assert.That(result, Has.Count.EqualTo(5));
        _api.Verify(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(lower)), default), Times.Once);
        _api.Verify(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(upper)), default), Times.Never);
    }

    [Test]
    public async Task LoadAssets_DeduplicatesAssetsWithMultipleTags()
    {
        var tag1 = Guid.NewGuid();
        var tag2 = Guid.NewGuid();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "Tag1", "Tag2" });

        _api.Setup(a => a.GetAllTagsAsync(default))
            .ReturnsAsync([
                new TagResponseDto { Id = tag1.ToString(), Name = "Tag1", Value = "Tag1" },
                new TagResponseDto { Id = tag2.ToString(), Name = "Tag2", Value = "Tag2" }
            ]);

        var shared = Asset("shared");
        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag1)), default))
            .ReturnsAsync(SearchResult([shared, Asset("tag1-only")]));
        _api.Setup(a => a.SearchAssetsAsync(It.Is<MetadataSearchDto>(d => d.TagIds.Contains(tag2)), default))
            .ReturnsAsync(SearchResult([shared, Asset("tag2-only")]));

        var result = (await _pool.LoadAssetsPublic()).ToList();

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Count(a => a.Id == "shared"), Is.EqualTo(1));

        // Verify that when an asset is displayed, all tags are shown (even though settings
        // only configured which tags to search, not which to display on assets)
        var sharedAsset = result.First(a => a.Id == "shared");
        Assert.Multiple(() =>
        {
            Assert.That(sharedAsset.Tags, Has.Count.EqualTo(2), "Asset should display both tags it was found in");
            Assert.That(sharedAsset.Tags!.Any(t => t.Id == tag1.ToString()), "Asset should show Tag1");
            Assert.That(sharedAsset.Tags!.Any(t => t.Id == tag2.ToString()), "Asset should show Tag2");
        });
    }

}
