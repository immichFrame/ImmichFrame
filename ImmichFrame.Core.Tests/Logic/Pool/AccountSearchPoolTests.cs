using System.Collections.Concurrent;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class AccountSearchPoolTests
{
    private Mock<IApiCache> _cache;
    private Mock<ImmichApi> _api;
    private Mock<IAccountSettings> _settings;
    private AccountSearchPool _pool;

    [SetUp]
    public void Setup()
    {
        _cache = new Mock<IApiCache>();
        _cache.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<Func<Task<ICollection<TagResponseDto>>>>()))
            .Returns<string, Func<Task<ICollection<TagResponseDto>>>>((_, factory) => factory());
        _cache.Setup(c => c.GetOrAddAsync(It.IsAny<string>(), It.IsAny<Func<Task<long>>>()))
            .Returns<string, Func<Task<long>>>((_, factory) => factory());
        _api = new Mock<ImmichApi>(null, null);
        _api.Setup(a => a.SearchRandomAsync(It.IsAny<RandomSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetResponseDto>());
        _api.Setup(a => a.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchStatisticsResponseDto { Total = 0 });
        _settings = new Mock<IAccountSettings>();
        _settings.SetupGet(s => s.ImmichServerUrl).Returns("https://photos.example");
        _settings.SetupGet(s => s.Albums).Returns(new List<Guid>());
        _settings.SetupGet(s => s.People).Returns(new List<Guid>());
        _settings.SetupGet(s => s.Tags).Returns(new List<string>());
        _settings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid>());
        _pool = new AccountSearchPool(_cache.Object, _api.Object, _settings.Object);
    }

    [TearDown]
    public void TearDown() => _pool.Dispose();

    [Test]
    public async Task GetAssets_WithoutSources_SearchesTheLibrary()
    {
        await _pool.GetAssets(5);

        VerifyRandom(dto =>
            dto.Size == 5 &&
            dto.WithExif == true &&
            dto.WithPeople == true &&
            dto.Filter!.Or == null &&
            dto.Filter.TrashedAt != null &&
            dto.Filter.AlbumIds == null &&
            dto.Filter.Visibility!.In.Contains(AssetVisibility.Timeline) &&
            !dto.Filter.Visibility.In.Contains(AssetVisibility.Archive) &&
            dto.Filter.Type!.In.Contains(AssetTypeEnum.IMAGE) &&
            !dto.Filter.Type.In.Contains(AssetTypeEnum.VIDEO));
    }

    [Test]
    public async Task GetAssets_ShowVideos_AddsVideoType()
    {
        _settings.SetupGet(s => s.ShowVideos).Returns(true);

        await _pool.GetAssets(1);

        VerifyRandom(dto =>
            dto.Filter!.Type!.In.Contains(AssetTypeEnum.IMAGE) &&
            dto.Filter.Type.In.Contains(AssetTypeEnum.VIDEO));
    }

    [Test]
    public async Task GetAssets_ShowArchived_AddsArchiveVisibility()
    {
        _settings.SetupGet(s => s.ShowArchived).Returns(true);

        await _pool.GetAssets(1);

        VerifyRandom(dto =>
            dto.Filter!.Visibility!.In.Contains(AssetVisibility.Timeline) &&
            dto.Filter.Visibility.In.Contains(AssetVisibility.Archive));
    }

    [Test]
    public async Task GetAssets_Rating_SetsGte()
    {
        _settings.SetupGet(s => s.Rating).Returns(3);

        await _pool.GetAssets(1);

        VerifyRandom(dto => dto.Filter!.Rating!.Gte == 3);
    }

    [Test]
    public async Task GetAssets_ImagesFromDays_SetsTakenAfter()
    {
        _settings.SetupGet(s => s.ImagesFromDays).Returns(10);
        var expected = DateTime.Today.AddDays(-10);

        await _pool.GetAssets(1);

        VerifyRandom(dto => dto.Filter!.TakenAt!.Gt.HasValue && dto.Filter.TakenAt.Gt.Value.Date == expected.Date);
    }

    [Test]
    public async Task GetAssets_ImagesFromDate_WinsOverFromDays()
    {
        var fromDate = new DateTime(2020, 6, 1);
        _settings.SetupGet(s => s.ImagesFromDate).Returns(fromDate);
        _settings.SetupGet(s => s.ImagesFromDays).Returns(10);

        await _pool.GetAssets(1);

        VerifyRandom(dto => dto.Filter!.TakenAt!.Gt.HasValue && dto.Filter.TakenAt.Gt.Value.Date == fromDate.Date);
    }

    [Test]
    public async Task GetAssets_ImagesUntilDate_SetsTakenBefore()
    {
        var until = new DateTime(2024, 1, 1);
        _settings.SetupGet(s => s.ImagesUntilDate).Returns(until);

        await _pool.GetAssets(1);

        VerifyRandom(dto => dto.Filter!.TakenAt!.Lt.HasValue && dto.Filter.TakenAt.Lt.Value.Date == until.Date);
    }

    [Test]
    public async Task GetAssets_ExcludedAlbums_SentAsNone()
    {
        var excludedAlbumId = Guid.NewGuid();
        _settings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid> { excludedAlbumId });

        await _pool.GetAssets(1);

        VerifyRandom(dto => dto.Filter!.AlbumIds!.None.Contains(excludedAlbumId));
        _api.Verify(a => a.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAssets_EmptyExcludedAlbums_OmitsAlbumFilter()
    {
        await _pool.GetAssets(1);

        VerifyRandom(dto => dto.Filter!.AlbumIds == null);
    }

    [Test]
    public async Task GetAssets_NullExcludedAlbums_OmitsAlbumFilter()
    {
        _settings.SetupGet(s => s.ExcludedAlbums).Returns((List<Guid>)null!);

        await _pool.GetAssets(1);

        VerifyRandom(dto => dto.Filter!.AlbumIds == null);
    }

    [Test]
    public async Task GetAssets_WithSources_OrsOnlyConfiguredBranches()
    {
        var albumId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var excludedAlbumId = Guid.NewGuid();
        _settings.SetupGet(s => s.ShowFavorites).Returns(true);
        _settings.SetupGet(s => s.Albums).Returns(new List<Guid> { albumId });
        _settings.SetupGet(s => s.People).Returns(new List<Guid> { personId });
        _settings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid> { excludedAlbumId });

        await _pool.GetAssets(1);

        VerifyRandom(dto =>
            dto.Filter!.AlbumIds!.None.Contains(excludedAlbumId) &&
            dto.Filter.Or.Count == 3 &&
            dto.Filter.Or.Any(branch => branch.IsFavorite != null && branch.IsFavorite.Eq) &&
            dto.Filter.Or.Any(branch => branch.AlbumIds != null && branch.AlbumIds.Any.Contains(albumId) && branch.AlbumIds.None == null) &&
            dto.Filter.Or.Any(branch => branch.PersonIds != null && branch.PersonIds.Any.Contains(personId)));
    }

    [Test]
    public async Task GetAssets_Tags_MatchFullPathCaseSensitively()
    {
        var match = Guid.NewGuid();
        var wrongCase = Guid.NewGuid();
        var wrongParent = Guid.NewGuid();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "Parent/Child" });
        _api.Setup(a => a.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagResponseDto>
            {
                new() { Id = match, Value = "Parent/Child" },
                new() { Id = wrongCase, Value = "parent/child" },
                new() { Id = wrongParent, Value = "Other/Child" }
            });

        await _pool.GetAssets(1);

        VerifyRandom(dto =>
            dto.Filter!.Or.Count == 1 &&
            dto.Filter.Or.Single().TagIds!.Any.Contains(match) &&
            !dto.Filter.Or.Single().TagIds.Any.Contains(wrongCase) &&
            !dto.Filter.Or.Single().TagIds.Any.Contains(wrongParent));
    }

    [Test]
    public async Task GetAssets_UnmatchedTagsOnly_DoesNotSearch()
    {
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "missing" });
        _api.Setup(a => a.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagResponseDto>());

        var result = await _pool.GetAssets(5);

        Assert.That(result, Is.Empty);
        _api.Verify(a => a.SearchRandomAsync(It.IsAny<RandomSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAssetCount_UsesTheSameFilter()
    {
        _settings.SetupGet(s => s.ShowFavorites).Returns(true);
        _api.Setup(a => a.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchStatisticsResponseDto { Total = 12 });

        var count = await _pool.GetAssetCount();

        Assert.That(count, Is.EqualTo(12));
        _api.Verify(a => a.SearchAssetStatisticsAsync(It.Is<StatisticsSearchDto>(dto =>
            dto.Filter!.Or.Any(branch => branch.IsFavorite != null && branch.IsFavorite.Eq) &&
            dto.Filter.Visibility!.In.Contains(AssetVisibility.Timeline)
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAssetCount_UnmatchedTagsOnly_IsZeroWithoutStatisticsCall()
    {
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "missing" });
        _api.Setup(a => a.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagResponseDto>());

        var count = await _pool.GetAssetCount();

        Assert.That(count, Is.EqualTo(0));
        _api.Verify(a => a.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAssetCount_SameFilter_IsServedFromCache()
    {
        using var cache = new ApiCache(TimeSpan.FromHours(1));
        var pool = new AccountSearchPool(cache, _api.Object, _settings.Object);
        _api.Setup(a => a.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchStatisticsResponseDto { Total = 7 });

        Assert.That(await pool.GetAssetCount(), Is.EqualTo(7));
        Assert.That(await pool.GetAssetCount(), Is.EqualTo(7));

        _api.Verify(a => a.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAllAssetIds_PagesWithTheCursor()
    {
        var excludedAlbumId = Guid.NewGuid();
        _settings.SetupGet(s => s.ShowFavorites).Returns(true);
        _settings.SetupGet(s => s.ExcludedAlbums).Returns(new List<Guid> { excludedAlbumId });
        var page1 = Ids("page1", 3);
        var page2 = Ids("page2", 2);
        var page3 = Ids("page3", 1);
        var requests = new List<MetadataSearchDto>();
        SetupSearch((dto, _) =>
        {
            requests.Add(dto);
            return Task.FromResult(dto.Cursor switch
            {
                null => SearchPage(page1, "cursor-1"),
                "cursor-1" => SearchPage(page2, "cursor-2"),
                "cursor-2" => SearchPage(page3, null),
                _ => throw new InvalidOperationException($"Unexpected cursor {dto.Cursor}")
            });
        });

        var ids = await _pool.GetAllAssetIds();

        Assert.That(ids.Ids, Is.EqualTo(page1.Concat(page2).Concat(page3)));
        Assert.That(requests, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(requests[0].Cursor, Is.Null);
            Assert.That(requests[1].Cursor, Is.EqualTo("cursor-1"));
            Assert.That(requests[2].Cursor, Is.EqualTo("cursor-2"));
            foreach (var dto in requests)
            {
                Assert.That(dto.Size, Is.EqualTo(1000));
                Assert.That(dto.WithExif, Is.False);
                Assert.That(dto.Filter!.Id, Is.Null);
                Assert.That(dto.Filter.Or.Any(branch => branch.IsFavorite != null && branch.IsFavorite.Eq), Is.True);
                Assert.That(dto.Filter.AlbumIds!.None, Does.Contain(excludedAlbumId));
                Assert.That(dto.Filter.TrashedAt, Is.Not.Null);
                Assert.That(dto.Filter.Visibility!.In, Does.Contain(AssetVisibility.Timeline));
                Assert.That(dto.Filter.Type!.In, Does.Contain(AssetTypeEnum.IMAGE));
            }
        });
        _api.Verify(a => a.SearchRandomAsync(It.IsAny<RandomSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllAssetIds_UnmatchedTagsOnly_IsEmptyWithoutSearch()
    {
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "missing" });
        _api.Setup(a => a.GetAllTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagResponseDto>());

        var ids = await _pool.GetAllAssetIds();

        Assert.That(ids.Ids, Is.Empty);
        _api.Verify(a => a.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllAssetIds_ConcurrentFirstCalls_ShareOneListing()
    {
        var ids = Ids("concurrent", 4);
        var release = new TaskCompletionSource();
        var listings = 0;
        SetupSearch(async (dto, _) =>
        {
            Interlocked.Increment(ref listings);
            await release.Task;
            return SearchPage(ids, null);
        });

        var calls = Enumerable.Range(0, 10).Select(_ => Task.Run(() => _pool.GetAllAssetIds())).ToList();
        await Task.Delay(50);
        release.SetResult();
        var results = await Task.WhenAll(calls);

        Assert.That(listings, Is.EqualTo(1));
        Assert.That(results, Is.All.SameAs(results[0]));
        Assert.That(results[0].Ids, Is.EqualTo(ids));
    }

    [Test]
    public async Task GetAllAssetIds_ZeroInterval_ListsAtMostOncePerFloor()
    {
        var time = new FakeTimeProvider();
        var listings = 0;
        SetupSearch((dto, _) =>
        {
            var n = Interlocked.Increment(ref listings);
            return Task.FromResult(SearchPage(Ids($"listing{n}", 3), null));
        });
        using var pool = new AccountSearchPool(_cache.Object, _api.Object, _settings.Object, TimeSpan.Zero, time);

        Assert.That(AccountSearchPool.MinIdListLifetime, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(pool.IdListLifetime, Is.EqualTo(AccountSearchPool.MinIdListLifetime));

        var first = await pool.GetAllAssetIds();
        for (var i = 0; i < 10; i++)
        {
            time.Now += TimeSpan.FromSeconds(29);
            Assert.That(await pool.GetAllAssetIds(), Is.SameAs(first));
        }

        Assert.That(listings, Is.EqualTo(1));

        // Past the floor, the old listing is still served while one refresh runs.
        time.Now += TimeSpan.FromSeconds(10);
        Assert.That(await pool.GetAllAssetIds(), Is.SameAs(first));
        var refreshed = await WaitFor(async () =>
        {
            var current = await pool.GetAllAssetIds();
            return ReferenceEquals(current, first) ? null : current;
        });

        Assert.That(listings, Is.EqualTo(2));
        Assert.That(refreshed.Version, Is.GreaterThan(first.Version));
    }

    [Test]
    public void IdListLifetime_LongerThanFloor_IsKept()
    {
        using var pool = new AccountSearchPool(_cache.Object, _api.Object, _settings.Object, TimeSpan.FromHours(12));

        Assert.That(pool.IdListLifetime, Is.EqualTo(TimeSpan.FromHours(12)));
    }

    [Test]
    public async Task GetAllAssetIds_CallerCancelled_SharedListingCarriesOn()
    {
        var ids = Ids("shared", 3);
        var release = new TaskCompletionSource();
        CancellationToken listingToken = default;
        SetupSearch(async (dto, ct) =>
        {
            listingToken = ct;
            await release.Task;
            return SearchPage(ids, null);
        });

        using var cts = new CancellationTokenSource();
        var cancelled = _pool.GetAllAssetIds(cts.Token);
        var other = _pool.GetAllAssetIds();
        await Task.Delay(20);
        cts.Cancel();

        Assert.That(async () => await cancelled, Throws.InstanceOf<OperationCanceledException>());
        Assert.That(listingToken.IsCancellationRequested, Is.False);

        release.SetResult();
        var result = await other;
        Assert.That(result.Ids, Is.EqualTo(ids));
        _api.Verify(a => a.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAssetsByIds_SearchesWithAccountFilterAndId()
    {
        var id = FixtureHelpers.GuidFor("one");
        _settings.SetupGet(s => s.ShowFavorites).Returns(true);
        var requests = new List<MetadataSearchDto>();
        SetupSearch((dto, _) =>
        {
            requests.Add(dto);
            return Task.FromResult(SearchPage(new List<Guid> { IdOf(dto) }, null));
        });

        var result = await _pool.LoadAssetsByIds(new List<Guid> { id });

        Assert.That(result.Loaded.Select(a => a.Id), Is.EqualTo(new[] { id }));
        Assert.That(requests, Has.Count.EqualTo(1));
        var request = requests[0];
        Assert.Multiple(() =>
        {
            Assert.That(request.Filter!.Id!.Eq, Is.EqualTo(id));
            Assert.That(request.Size, Is.EqualTo(1));
            Assert.That(request.WithExif, Is.True);
            Assert.That(request.WithPeople, Is.True);
            Assert.That(request.Filter.Or.Any(branch => branch.IsFavorite != null && branch.IsFavorite.Eq), Is.True);
            Assert.That(request.Filter.TrashedAt, Is.Not.Null);
        });
        _api.Verify(a => a.GetAssetInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task LoadAssetsByIds_KeepsTheInputOrder()
    {
        var ids = Ids("ordered", 8);
        SetupLoads(async id =>
        {
            // Later IDs answer sooner, so completion order differs from input order.
            await Task.Delay((ids.Count - ids.IndexOf(id)) * 10);
            return Asset(id);
        });

        var result = await _pool.LoadAssetsByIds(ids);

        Assert.That(result.Loaded.Select(a => a.Id), Is.EqualTo(ids));
        Assert.That(result.Retry, Is.Empty);
    }

    [Test]
    public async Task LoadAssetsByIds_SkipsDeletedTrashedAndUnselectedAssets()
    {
        var kept1 = FixtureHelpers.GuidFor("kept1");
        var deleted = FixtureHelpers.GuidFor("deleted");
        var trashed = FixtureHelpers.GuidFor("trashed");
        var unselected = FixtureHelpers.GuidFor("unselected");
        var kept2 = FixtureHelpers.GuidFor("kept2");
        SetupLoads(id =>
            id == deleted ? throw ApiError(404)
            : id == trashed ? Task.FromResult<AssetResponseDto?>(new AssetResponseDto { Id = trashed, Type = AssetTypeEnum.IMAGE, IsTrashed = true })
            : id == unselected ? Task.FromResult<AssetResponseDto?>(null)
            : Task.FromResult<AssetResponseDto?>(Asset(id)));

        var result = await _pool.LoadAssetsByIds(new List<Guid> { kept1, deleted, trashed, unselected, kept2 });

        Assert.That(result.Loaded.Select(a => a.Id), Is.EqualTo(new[] { kept1, kept2 }));
        Assert.That(result.Retry, Is.Empty);
    }

    [TestCase(400)]
    [TestCase(403)]
    [TestCase(404)]
    public async Task LoadAssetsByIds_ClientErrorMidBatch_DropsOnlyThatAsset(int status)
    {
        var before = FixtureHelpers.GuidFor("before");
        var failing = FixtureHelpers.GuidFor("failing");
        var after = FixtureHelpers.GuidFor("after");
        SetupLoads(id => id == failing ? throw ApiError(status) : Task.FromResult<AssetResponseDto?>(Asset(id)));

        var result = await _pool.LoadAssetsByIds(new List<Guid> { before, failing, after });

        Assert.That(result.Loaded.Select(a => a.Id), Is.EqualTo(new[] { before, after }));
        Assert.That(result.Retry, Is.Empty);
    }

    [Test]
    public async Task LoadAssetsByIds_OtherErrors_AreRetriedAndLoggedOncePerBatch()
    {
        var kept = FixtureHelpers.GuidFor("kept");
        var serverError = FixtureHelpers.GuidFor("server-error");
        var networkError = FixtureHelpers.GuidFor("network-error");
        SetupLoads(id =>
            id == serverError ? throw ApiError(500)
            : id == networkError ? throw new HttpRequestException("connection reset")
            : Task.FromResult<AssetResponseDto?>(Asset(id)));
        var logger = new Mock<ILogger>();
        using var pool = new AccountSearchPool(_cache.Object, _api.Object, _settings.Object, logger: logger.Object);

        var result = await pool.LoadAssetsByIds(new List<Guid> { serverError, kept, networkError });

        Assert.That(result.Loaded.Select(a => a.Id), Is.EqualTo(new[] { kept }));
        Assert.That(result.Retry, Is.EqualTo(new[] { serverError, networkError }));
        logger.Verify(l => l.Log(Microsoft.Extensions.Logging.LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Could not load 2 of 3 assets")),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        logger.Verify(l => l.Log(Microsoft.Extensions.Logging.LogLevel.Warning, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public void LoadAssetsByIds_CallerCancelled_Throws()
    {
        var ids = Ids("cancel", 3);
        SetupSearch(async (dto, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return SearchPage(new List<Guid> { IdOf(dto) }, null);
        });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        Assert.That(async () => await _pool.LoadAssetsByIds(ids, cts.Token), Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task GetAssets_RandomMode_NeverListsOrLoadsById()
    {
        await _pool.GetAssets(25, "frame-1");

        VerifyRandom(dto => dto.Size == 25);
        _api.Verify(a => a.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
        _api.Verify(a => a.GetAssetInfoAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAssets_ExhaustiveMode_ListsAndLoadsByIdWithoutRandomSearch()
    {
        var ids = Ids("exhaustive", 10);
        var listings = 0;
        var loads = 0;
        SetupSearch((dto, _) =>
        {
            if (dto.Filter!.Id == null)
            {
                Interlocked.Increment(ref listings);
                return Task.FromResult(SearchPage(ids, null));
            }

            Interlocked.Increment(ref loads);
            return Task.FromResult(SearchPage(new List<Guid> { IdOf(dto) }, null));
        });

        var dealer = new ShuffleDeckPool(_pool);

        var result = (await dealer.GetAssets(5, "frame-1")).ToList();

        Assert.That(result, Has.Count.EqualTo(5));
        Assert.That(result.Select(a => a.Id), Is.Unique);
        Assert.That(ids, Is.SupersetOf(result.Select(a => a.Id)));
        Assert.That(listings, Is.EqualTo(1));
        Assert.That(loads, Is.EqualTo(5));
        _api.Verify(a => a.SearchRandomAsync(It.IsAny<RandomSearchDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAssets_ExhaustiveMode_AssetLeftFilterSinceListing_IsSkippedAndBatchToppedUp()
    {
        var ids = Ids("filtered", 10);
        Guid? leftFilter = null;
        SetupSearch((dto, _) =>
        {
            if (dto.Filter!.Id == null)
            {
                return Task.FromResult(SearchPage(ids, null));
            }

            // The first asset dealt has left the selection (say, unfavourited) since the listing.
            var id = IdOf(dto);
            bool excluded;
            lock (ids)
            {
                leftFilter ??= id;
                excluded = leftFilter == id;
            }

            return Task.FromResult(SearchPage(excluded ? new List<Guid>() : new List<Guid> { id }, null));
        });
        var dealer = new ShuffleDeckPool(_pool);

        var result = (await dealer.GetAssets(5, "frame-1")).Select(a => a.Id).ToList();

        Assert.That(leftFilter, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(5));
        Assert.That(result, Is.Unique);
        Assert.That(result, Does.Not.Contain(leftFilter!.Value));
    }

    [Test]
    public async Task LoadAssetsByIds_ConcurrentCallers_ShareOneLoadCap()
    {
        var running = 0;
        var peak = 0;
        SetupLoads(async id =>
        {
            var now = Interlocked.Increment(ref running);
            InterlockedMax(ref peak, now);
            await Task.Delay(20);
            Interlocked.Decrement(ref running);
            return Asset(id);
        });

        var callers = Enumerable.Range(0, 6)
            .Select(c => Task.Run(() => _pool.LoadAssetsByIds(Ids($"caller{c}", 8))))
            .ToList();
        var results = await Task.WhenAll(callers);

        Assert.That(results.Sum(r => r.Loaded.Count), Is.EqualTo(48));
        Assert.That(peak, Is.LessThanOrEqualTo(AccountSearchPool.MaxParallelLoads));
    }

    [Test]
    public async Task LoadAssetsByIds_SameIdWithinCacheLifetime_IsNotSearchedAgain()
    {
        var time = new FakeTimeProvider();
        using var pool = new AccountSearchPool(_cache.Object, _api.Object, _settings.Object, timeProvider: time);
        var id = FixtureHelpers.GuidFor("cached");
        var gone = FixtureHelpers.GuidFor("gone");
        var searches = new ConcurrentDictionary<Guid, int>();
        SetupLoads(asked =>
        {
            searches.AddOrUpdate(asked, 1, (_, n) => n + 1);
            return asked == gone ? throw ApiError(404) : Task.FromResult<AssetResponseDto?>(Asset(asked));
        });

        await pool.LoadAssetsByIds(new List<Guid> { id, gone });
        time.Now += AccountSearchPool.LoadCacheLifetime - TimeSpan.FromSeconds(1);
        var again = await pool.LoadAssetsByIds(new List<Guid> { gone, id });

        Assert.That(again.Loaded.Select(a => a.Id), Is.EqualTo(new[] { id }));
        Assert.That(searches[id], Is.EqualTo(1));
        Assert.That(searches[gone], Is.EqualTo(1));

        time.Now += TimeSpan.FromSeconds(1);
        await pool.LoadAssetsByIds(new List<Guid> { id });

        Assert.That(searches[id], Is.EqualTo(2));
    }

    [Test]
    public async Task LoadAssetsByIds_RetriedFailure_IsNotCached()
    {
        var id = FixtureHelpers.GuidFor("flaky");
        var searches = 0;
        SetupLoads(asked => Interlocked.Increment(ref searches) == 1
            ? throw ApiError(500)
            : Task.FromResult<AssetResponseDto?>(Asset(asked)));

        var first = await _pool.LoadAssetsByIds(new List<Guid> { id });
        var second = await _pool.LoadAssetsByIds(new List<Guid> { id });

        Assert.That(first.Retry, Is.EqualTo(new[] { id }));
        Assert.That(second.Loaded.Select(a => a.Id), Is.EqualTo(new[] { id }));
        Assert.That(searches, Is.EqualTo(2));
    }

    [TestCase(400)]
    [TestCase(403)]
    public async Task LoadAssetsByIds_RefusedRunAcrossOneAssetCalls_WarnsAndKeepsAssets(int status)
    {
        var ids = Ids("refused", AccountSearchPool.RefusalsBeforeWarning);
        SetupLoads(_ => throw ApiError(status));
        var logger = new Mock<ILogger>();
        using var pool = new AccountSearchPool(_cache.Object, _api.Object, _settings.Object, logger: logger.Object);

        var results = new List<AssetLoadResult>();
        foreach (var id in ids)
        {
            results.Add(await pool.LoadAssetsByIds(new List<Guid> { id }));
        }

        Assert.That(results.SelectMany(r => r.Loaded), Is.Empty);
        // Until the run is long enough, each refused asset is taken to be gone and dropped.
        Assert.That(results.SkipLast(1).SelectMany(r => r.Retry), Is.Empty);
        Assert.That(results[^1].Retry, Is.EqualTo(new[] { ids[^1] }));
        VerifyRefusalWarning(logger, Times.Once());
    }

    [Test]
    public async Task LoadAssetsByIds_TwoDeletedAssets_AreDroppedWithoutWarning()
    {
        var ids = Ids("deleted", 2);
        SetupLoads(_ => throw ApiError(400));
        var logger = new Mock<ILogger>();
        using var pool = new AccountSearchPool(_cache.Object, _api.Object, _settings.Object, logger: logger.Object);

        var result = await pool.LoadAssetsByIds(ids);

        Assert.That(result.Loaded, Is.Empty);
        Assert.That(result.Retry, Is.Empty);
        VerifyRefusalWarning(logger, Times.Never());
    }

    [Test]
    public async Task LoadAssetsByIds_SuccessfulLoad_EndsTheRefusalRun()
    {
        var refused = Ids("refused", AccountSearchPool.RefusalsBeforeWarning * 2 - 2);
        var kept = FixtureHelpers.GuidFor("kept");
        SetupLoads(id => id == kept ? Task.FromResult<AssetResponseDto?>(Asset(id)) : throw ApiError(403));
        var logger = new Mock<ILogger>();
        using var pool = new AccountSearchPool(_cache.Object, _api.Object, _settings.Object, logger: logger.Object);

        var half = AccountSearchPool.RefusalsBeforeWarning - 1;
        var calls = refused.Take(half).Append(kept).Concat(refused.Skip(half));
        var retried = new List<Guid>();
        foreach (var id in calls)
        {
            retried.AddRange((await pool.LoadAssetsByIds(new List<Guid> { id })).Retry);
        }

        Assert.That(retried, Is.Empty);
        VerifyRefusalWarning(logger, Times.Never());
    }

    [Test]
    public void GetAllAssetIds_CursorKeepsGoing_GivesUpPastThePageCap()
    {
        _api.Setup(a => a.SearchAssetStatisticsAsync(It.IsAny<StatisticsSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchStatisticsResponseDto { Total = 2500 });
        var pages = 0;
        // Far more pages than 2500 assets fill; ends only so the test cannot hang.
        SetupSearch((_, _) =>
        {
            var page = Interlocked.Increment(ref pages);
            return Task.FromResult(SearchPage(Ids($"page{page}", 1), page < 50 ? $"cursor-{page}" : null));
        });

        Assert.That(async () => await _pool.GetAllAssetIds(), Throws.InstanceOf<InvalidOperationException>());
        // 2500 assets need 3 pages; the cap allows 2 + 10.
        Assert.That(pages, Is.EqualTo(12));
    }

    [Test]
    public async Task GetAllAssetIds_SelectsNothingThenSomething_RealListingIsNewer()
    {
        var tags = new List<TagResponseDto>();
        _settings.SetupGet(s => s.Tags).Returns(new List<string> { "later" });
        _api.Setup(a => a.GetAllTagsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => (ICollection<TagResponseDto>)tags.ToList());
        SetupSearch((_, _) => Task.FromResult(SearchPage(Ids("tagged", 3), null)));

        var empty = await _pool.GetAllAssetIds();
        var emptyAgain = await _pool.GetAllAssetIds();
        tags.Add(new TagResponseDto { Id = FixtureHelpers.GuidFor("tag"), Value = "later" });
        var listing = await _pool.GetAllAssetIds();
        tags.Clear();
        var emptyAfter = await _pool.GetAllAssetIds();

        Assert.Multiple(() =>
        {
            Assert.That(emptyAgain, Is.SameAs(empty));
            Assert.That(emptyAfter, Is.SameAs(empty));
            Assert.That(listing.Count, Is.EqualTo(3));
            Assert.That(listing.Version, Is.GreaterThan(empty.Version));
        });

        // A deck synced to the real listing keeps it when the empty one comes round again.
        var deck = new ShuffleDeck(new Random(1));
        deck.Sync(listing);
        Assert.That(deck.Sync(emptyAfter), Is.False);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int seen;
        while ((seen = Volatile.Read(ref target)) < value && Interlocked.CompareExchange(ref target, value, seen) != seen)
        {
        }
    }

    private static List<Guid> Ids(string prefix, int count) =>
        Enumerable.Range(0, count).Select(i => FixtureHelpers.GuidFor($"{prefix}_{i}")).ToList();

    private static AssetResponseDto Asset(Guid id) => new()
    {
        Id = id,
        Type = AssetTypeEnum.IMAGE,
        IsTrashed = false,
        ExifInfo = new ExifResponseDto()
    };

    // The asset an ID-filtered search asks for.
    private static Guid IdOf(MetadataSearchDto dto) => dto.Filter!.Id!.Eq!.Value;

    private static void VerifyRefusalWarning(Mock<ILogger> logger, Times times) =>
        logger.Verify(l => l.Log(Microsoft.Extensions.Logging.LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("refused the last")),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);

    private void SetupSearch(Func<MetadataSearchDto, CancellationToken, Task<SearchResponseDto>> handler) =>
        _api.Setup(a => a.SearchAssetsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()))
            .Returns((string key, string slug, MetadataSearchDto dto, CancellationToken ct) => handler(dto, ct));

    // Answers each ID-filtered search with the asset the loader gives, or with no match for null.
    private void SetupLoads(Func<Guid, Task<AssetResponseDto?>> load) =>
        SetupSearch(async (dto, _) =>
        {
            var asset = await load(IdOf(dto));
            return SearchPageOf(asset == null ? new List<AssetResponseDto>() : new List<AssetResponseDto> { asset });
        });

    private static async Task<T> WaitFor<T>(Func<Task<T?>> probe) where T : class
    {
        for (var i = 0; i < 200; i++)
        {
            if (await probe() is { } value)
            {
                return value;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException();
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static ApiException ApiError(int statusCode) =>
        new($"Status {statusCode}", statusCode, string.Empty, new Dictionary<string, IEnumerable<string>>(), null);

    internal static SearchResponseDto SearchPage(IReadOnlyCollection<Guid> ids, string? nextCursor) =>
        SearchPageOf(ids.Select(id => new AssetResponseDto { Id = id, Type = AssetTypeEnum.IMAGE }).ToList(), nextCursor);

    private static SearchResponseDto SearchPageOf(List<AssetResponseDto> assets, string? nextCursor = null) => new()
    {
        Albums = new SearchAlbumResponseDto
        {
            Count = 0,
            Total = 0,
            Items = new List<AlbumResponseDto>(),
            Facets = new List<SearchFacetResponseDto>()
        },
        Assets = new SearchAssetResponseDto
        {
            Count = assets.Count,
            Total = assets.Count,
            Items = assets,
            Facets = new List<SearchFacetResponseDto>(),
            NextCursor = nextCursor,
            NextPage = null
        }
    };

    private void VerifyRandom(Func<RandomSearchDto, bool> match) =>
        _api.Verify(a => a.SearchRandomAsync(It.Is<RandomSearchDto>(dto => match(dto)), It.IsAny<CancellationToken>()), Times.Once);
}
