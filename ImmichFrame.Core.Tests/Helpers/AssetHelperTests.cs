using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Tests.Logic.Pool;

namespace ImmichFrame.Core.Tests.Helpers;

[TestFixture]
public class AssetHelperTests
{
    private Mock<ImmichApi> _mockImmichApi;
    private Mock<IAccountSettings> _mockAccountSettings;

    [SetUp]
    public void Setup()
    {
        _mockImmichApi = new Mock<ImmichApi>(null, null);
        _mockAccountSettings = new Mock<IAccountSettings>();
    }

    private SearchResponseDto CreateSearchResult(List<AssetResponseDto> assets) =>
        new SearchResponseDto { Assets = new SearchAssetResponseDto { Items = assets, Total = assets.Count } };

    private void SetupPage(Guid personId, int page, List<AssetResponseDto> assets) =>
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.Is<MetadataSearchDto>(d => d.PersonIds != null && d.PersonIds.Contains(personId) && d.Page == page),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(assets));

    [Test]
    public async Task GetExcludedPeopleAssets_FetchesEveryPage_WhenPersonExceedsOneBatch()
    {
        var personId = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.ExcludedPeople).Returns(new List<Guid> { personId });

        var batchSize = 1000;
        var page1 = Enumerable.Range(0, batchSize).Select(i => new AssetResponseDto { Id = FixtureHelpers.GuidFor($"p1_{i}") }).ToList();
        var page2 = Enumerable.Range(0, 30).Select(i => new AssetResponseDto { Id = FixtureHelpers.GuidFor($"p2_{i}") }).ToList();
        SetupPage(personId, 1, page1);
        SetupPage(personId, 2, page2);

        var result = (await AssetHelper.GetExcludedPeopleAssets(_mockImmichApi.Object, _mockAccountSettings.Object)).ToList();

        Assert.That(result.Count, Is.EqualTo(batchSize + 30),
            "a person with more than one batch of assets must be fetched across every page");
        Assert.That(result.Any(a => a.Id == FixtureHelpers.GuidFor("p2_29")));
    }

    [Test]
    public async Task GetExcludedPeopleAssets_AggregatesAcrossPeople()
    {
        var person1 = Guid.NewGuid();
        var person2 = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.ExcludedPeople).Returns(new List<Guid> { person1, person2 });

        SetupPage(person1, 1, new List<AssetResponseDto> { new AssetResponseDto { Id = FixtureHelpers.GuidFor("a") } });
        SetupPage(person2, 1, new List<AssetResponseDto> { new AssetResponseDto { Id = FixtureHelpers.GuidFor("b") } });

        var result = (await AssetHelper.GetExcludedPeopleAssets(_mockImmichApi.Object, _mockAccountSettings.Object)).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(a => a.Id == FixtureHelpers.GuidFor("a")));
        Assert.That(result.Any(a => a.Id == FixtureHelpers.GuidFor("b")));
    }

    [Test]
    public async Task GetExcludedPeopleAssets_NoExcludedPeople_MakesNoApiCalls()
    {
        _mockAccountSettings.SetupGet(s => s.ExcludedPeople).Returns(new List<Guid>());

        var result = (await AssetHelper.GetExcludedPeopleAssets(_mockImmichApi.Object, _mockAccountSettings.Object)).ToList();

        Assert.That(result, Is.Empty);
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetExcludedPeopleAssets_NullExcludedPeople_ReturnsEmpty()
    {
        _mockAccountSettings.SetupGet(s => s.ExcludedPeople).Returns((List<Guid>)null);

        var result = (await AssetHelper.GetExcludedPeopleAssets(_mockImmichApi.Object, _mockAccountSettings.Object)).ToList();

        Assert.That(result, Is.Empty);
    }
}
