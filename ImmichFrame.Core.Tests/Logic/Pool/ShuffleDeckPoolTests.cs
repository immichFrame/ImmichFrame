using ImmichFrame.Core.Api;
using ImmichFrame.Core.Logic.Pool;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class ShuffleDeckPoolTests
{
    private FakeDealablePool _source;
    private FakeTimeProvider _time;
    private ShuffleDeckPool _pool;

    private sealed class FakeDealablePool : IDealableAssetPool
    {
        private int _getAllAssetIdsCalls;
        private int _loadAssetsByIdsCalls;

        // Kept as the same instance until a test swaps it, like a cache that has not refreshed.
        public List<Guid> Ids { get; set; } = [];

        // IDs that LoadAssetsByIds leaves out, as if deleted or trashed.
        public HashSet<Guid> LeftOut { get; } = [];

        // Optional extra filter per call: returns the IDs of this request to leave out.
        public Func<int, IReadOnlyList<Guid>, IEnumerable<Guid>>? LeaveOut { get; set; }

        // IDs that LoadAssetsByIds reports as worth retrying, as if the server failed for a moment.
        public Func<int, IReadOnlyList<Guid>, IEnumerable<Guid>>? Retry { get; set; }

        // Throws from LoadAssetsByIds on the given call, as if the request was cancelled.
        public int? CancelOnCall { get; set; }

        public int GetAllAssetIdsCalls => Volatile.Read(ref _getAllAssetIdsCalls);
        public int LoadAssetsByIdsCalls => Volatile.Read(ref _loadAssetsByIdsCalls);
        public List<IReadOnlyList<Guid>> LoadRequests { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];

        private List<Guid>? _listedIds;
        private AssetIdList? _listing;

        public Task<long> GetAssetCount(CancellationToken ct = default) => Task.FromResult((long)Ids.Count);

        public Task<IEnumerable<AssetResponseDto>> GetAssets(int requested, string clientId = "", CancellationToken ct = default) =>
            throw new NotSupportedException();

        // One listing per Ids instance, as a cached source would hand out.
        public AssetIdList Listing
        {
            get
            {
                lock (LoadRequests)
                {
                    if (!ReferenceEquals(_listedIds, Ids))
                    {
                        _listedIds = Ids;
                        _listing = new AssetIdList(Ids);
                    }

                    return _listing!;
                }
            }
        }

        public Task<AssetIdList> GetAllAssetIds(CancellationToken ct = default)
        {
            Interlocked.Increment(ref _getAllAssetIdsCalls);
            return Task.FromResult(Listing);
        }

        public async Task<AssetLoadResult> LoadAssetsByIds(IReadOnlyList<Guid> ids, CancellationToken ct = default)
        {
            var call = Interlocked.Increment(ref _loadAssetsByIdsCalls) - 1;
            lock (LoadRequests)
            {
                LoadRequests.Add(ids.ToList());
                Tokens.Add(ct);
            }

            await Task.Yield();

            if (call == CancelOnCall)
            {
                throw new OperationCanceledException();
            }

            var leaveOut = new HashSet<Guid>(LeftOut);
            if (LeaveOut != null)
            {
                leaveOut.UnionWith(LeaveOut(call, ids));
            }

            var retry = Retry?.Invoke(call, ids).ToHashSet() ?? [];
            var loaded = ids.Where(id => !leaveOut.Contains(id) && !retry.Contains(id))
                .Select(id => new AssetResponseDto { Id = id }).ToList();
            return new AssetLoadResult(loaded, ids.Where(retry.Contains).ToList());
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static List<Guid> Ids(int count, string prefix = "id") =>
        Enumerable.Range(0, count).Select(i => FixtureHelpers.GuidFor($"{prefix}{i}")).ToList();

    [SetUp]
    public void Setup()
    {
        _source = new FakeDealablePool();
        _time = new FakeTimeProvider();
        _pool = new ShuffleDeckPool(_source, _time);
    }

    private async Task<List<Guid>> Draw(int requested, string clientId, int calls = 1)
    {
        var dealt = new List<Guid>();
        for (var i = 0; i < calls; i++)
        {
            var assets = (await _pool.GetAssets(requested, clientId)).ToList();
            Assert.That(assets, Has.Count.EqualTo(requested), $"client '{clientId}', call {i}");
            dealt.AddRange(assets.Select(a => a.Id));
        }

        return dealt;
    }

    [Test]
    public async Task GetAssetCount_ReturnsSourceCount()
    {
        _source.Ids = Ids(17);

        Assert.That(await _pool.GetAssetCount(), Is.EqualTo(17));
    }

    [Test]
    public async Task GetAssets_NonPositiveRequest_ReturnsEmpty()
    {
        _source.Ids = Ids(10);

        Assert.That(await _pool.GetAssets(0, "a"), Is.Empty);
        Assert.That(await _pool.GetAssets(-1, "a"), Is.Empty);
        Assert.That(_source.LoadAssetsByIdsCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAssets_EmptySource_ReturnsEmpty()
    {
        Assert.That(await _pool.GetAssets(5, "a"), Is.Empty);
        Assert.That(_source.LoadAssetsByIdsCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAssets_TwoClients_EachGetFullCoverageIndependently()
    {
        var ids = Ids(20);
        _source.Ids = ids;

        var aFirst = await Draw(5, "a", calls: 3);
        var b = await Draw(5, "b", calls: 4);
        var aRest = await Draw(5, "a", calls: 1);

        Assert.That(b, Is.EquivalentTo(ids));
        Assert.That(aFirst.Concat(aRest), Is.EquivalentTo(ids));
        Assert.That(_pool.DeckCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAssets_EmptyOrWhitespaceClientId_ShareOneDeck()
    {
        var ids = Ids(20);
        _source.Ids = ids;

        var dealt = new List<Guid>();
        dealt.AddRange(await Draw(5, ""));
        dealt.AddRange(await Draw(5, "   "));
        dealt.AddRange(await Draw(5, null!));
        dealt.AddRange(await Draw(5, "\t"));

        // They deal from the shared deck, which is not a client deck.
        Assert.That(_pool.DeckCount, Is.EqualTo(0));
        Assert.That(dealt, Is.EquivalentTo(ids));
    }

    [Test]
    public void NormalizeClientId_TrimsAndTruncates()
    {
        Assert.That(ShuffleDeckPool.NormalizeClientId(null), Is.EqualTo(string.Empty));
        Assert.That(ShuffleDeckPool.NormalizeClientId("  "), Is.EqualTo(string.Empty));
        Assert.That(ShuffleDeckPool.NormalizeClientId(" kitchen "), Is.EqualTo("kitchen"));

        var longId = new string('x', ShuffleDeckPool.MaxClientIdLength + 10);
        Assert.That(ShuffleDeckPool.NormalizeClientId(longId), Has.Length.EqualTo(ShuffleDeckPool.MaxClientIdLength));
    }

    [Test]
    public async Task GetAssets_ClientIdsEqualInFirst64Chars_ShareOneDeck()
    {
        var ids = Ids(20);
        _source.Ids = ids;
        var prefix = new string('x', ShuffleDeckPool.MaxClientIdLength);

        var dealt = new List<Guid>();
        dealt.AddRange(await Draw(5, prefix + "A", calls: 2));
        dealt.AddRange(await Draw(5, prefix + "B", calls: 2));

        Assert.That(_pool.DeckCount, Is.EqualTo(1));
        Assert.That(dealt, Is.EquivalentTo(ids));
    }

    [Test]
    public async Task GetAssets_DeckIdleMoreThan24Hours_IsDroppedAndStartsNewCycle()
    {
        var ids = Ids(20);
        _source.Ids = ids;

        await Draw(5, "a");
        await Draw(5, "b");
        Assert.That(_pool.DeckCount, Is.EqualTo(2));

        // Exactly the expiry is not yet idle for too long.
        _time.Now += ShuffleDeckPool.IdleExpiry;
        await Draw(5, "b");
        Assert.That(_pool.DeckCount, Is.EqualTo(2));

        _time.Now += TimeSpan.FromSeconds(1);
        await Draw(5, "b");
        Assert.That(_pool.DeckCount, Is.EqualTo(1), "a's deck should have expired");

        // a starts over with a fresh cycle that covers every asset.
        var aAgain = await Draw(5, "a", calls: 4);
        Assert.That(aAgain, Is.EquivalentTo(ids));
        Assert.That(_pool.DeckCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAssets_MoreClientsThanCap_EvictsLeastRecentlyUsed()
    {
        var ids = Ids(200);
        _source.Ids = ids;
        var firstDraws = new Dictionary<string, List<Guid>>();

        for (var i = 0; i < ShuffleDeckPool.MaxDecks; i++)
        {
            _time.Now += TimeSpan.FromMinutes(1);
            firstDraws[$"c{i}"] = await Draw(5, $"c{i}");
        }

        Assert.That(_pool.DeckCount, Is.EqualTo(ShuffleDeckPool.MaxDecks));

        // c0 is the oldest deck, but using it again makes c1 the least recently used.
        _time.Now += TimeSpan.FromMinutes(1);
        firstDraws["c0"].AddRange(await Draw(5, "c0"));

        _time.Now += TimeSpan.FromMinutes(1);
        await Draw(5, $"c{ShuffleDeckPool.MaxDecks}");
        Assert.That(_pool.DeckCount, Is.EqualTo(ShuffleDeckPool.MaxDecks));

        // c0 kept its deck: the rest of its cycle completes it with no repeat.
        _time.Now += TimeSpan.FromMinutes(1);
        var c0Rest = await Draw(5, "c0", calls: 38);
        Assert.That(firstDraws["c0"].Concat(c0Rest), Is.EquivalentTo(ids));

        // c1 lost its deck: a fresh deck deals from all 200 again, so its next 195 overlap its first 5.
        // (A fresh shuffle avoiding all 5 has odds of 1 in C(200, 5).)
        _time.Now += TimeSpan.FromMinutes(1);
        var c1Rest = await Draw(5, "c1", calls: 39);
        Assert.That(firstDraws["c1"].Concat(c1Rest), Is.Not.Unique);
        Assert.That(_pool.DeckCount, Is.EqualTo(ShuffleDeckPool.MaxDecks));
    }

    [Test]
    public async Task GetAssets_NewIdListInstance_IsSyncedMidCycle()
    {
        var ids = Ids(20);
        _source.Ids = ids;

        var before = await Draw(5, "a");

        var removed = ids.Except(before).First();
        var added = FixtureHelpers.GuidFor("added");
        var refreshed = ids.Where(id => id != removed).Append(added).ToList();
        _source.Ids = refreshed;

        // 15 were left in the cycle: one removed, one added.
        var restOfCycle = await Draw(5, "a", calls: 3);
        Assert.That(restOfCycle, Does.Contain(added));
        Assert.That(restOfCycle, Does.Not.Contain(removed));
        Assert.That(before.Concat(restOfCycle), Is.EquivalentTo(ids.Except(new[] { removed }).Append(added)));

        var later = await Draw(5, "a", calls: 8);
        Assert.That(later, Does.Not.Contain(removed));
        Assert.That(later.Take(20), Is.EquivalentTo(refreshed));
        Assert.That(later.Skip(20), Is.EquivalentTo(refreshed));
    }

    [Test]
    public async Task GetAssets_SameIdListInstance_IsNotResynced()
    {
        var ids = Ids(20);
        _source.Ids = ids;

        var dealt = await Draw(5, "a", calls: 4);
        Assert.That(dealt, Is.EquivalentTo(ids.ToList()));

        // Changing the same instance in place is not noticed: the deck only syncs on a new instance.
        var sneaked = FixtureHelpers.GuidFor("sneaked");
        ids.Add(sneaked);
        var sameInstance = await Draw(5, "a", calls: 12);
        Assert.That(sameInstance, Does.Not.Contain(sneaked));

        _source.Ids = ids.ToList();
        var newInstance = await Draw(1, "a", calls: 21);
        Assert.That(newInstance, Does.Contain(sneaked));
    }

    [Test]
    public async Task GetAssets_SomeAssetsMissing_TopsUpBatch()
    {
        var ids = Ids(20);
        _source.Ids = ids;
        _source.LeaveOut = (call, requested) => call == 0 ? requested.Take(2) : Enumerable.Empty<Guid>();

        var assets = (await _pool.GetAssets(5, "a")).Select(a => a.Id).ToList();

        Assert.That(assets, Has.Count.EqualTo(5));
        Assert.That(assets, Is.Unique);
        Assert.That(_source.LoadAssetsByIdsCalls, Is.EqualTo(2));
        Assert.That(_source.LoadRequests[0], Has.Count.EqualTo(5));
        Assert.That(_source.LoadRequests[1], Has.Count.EqualTo(2));
        Assert.That(assets, Is.EquivalentTo(_source.LoadRequests[0].Skip(2).Concat(_source.LoadRequests[1])));
    }

    [Test]
    public async Task GetAssets_AllAssetsMissing_StopsAtTheLoadBudget()
    {
        var ids = Ids(100);
        _source.Ids = ids;
        _source.LeftOut.UnionWith(ids);

        var assets = await _pool.GetAssets(5, "a");

        Assert.That(assets, Is.Empty);
        var loaded = _source.LoadRequests.SelectMany(r => r).ToList();
        Assert.That(loaded, Has.Count.EqualTo(5 * ShuffleDeckPool.LoadBudgetPerAsset));
        Assert.That(loaded, Is.Unique);
    }

    [Test]
    public async Task GetAssets_SomeAssetsMissing_TopUpNeverExceedsTheLoadBudget()
    {
        var ids = Ids(100);
        _source.Ids = ids;
        // Only one asset in three can be loaded, so the budget runs out before the batch fills.
        _source.LeaveOut = (_, requested) => requested.Where((_, i) => i % 3 != 0);

        var assets = await _pool.GetAssets(6, "a");

        Assert.That(_source.LoadRequests.Sum(r => r.Count), Is.EqualTo(12));
        Assert.That(assets.Count(), Is.LessThan(6));
    }

    [Test]
    public async Task GetAssets_ParallelCallsOneClient_CoverEveryAssetOnce()
    {
        var ids = Ids(200);
        _source.Ids = ids;

        var tasks = Enumerable.Range(0, 40).Select(_ => Task.Run(() => _pool.GetAssets(5, "a"))).ToList();
        var results = await Task.WhenAll(tasks);

        var dealt = results.SelectMany(r => r).Select(a => a.Id).ToList();
        Assert.That(dealt, Has.Count.EqualTo(200));
        Assert.That(dealt, Is.Unique);
        Assert.That(dealt, Is.EquivalentTo(ids));
    }

    [Test]
    public async Task GetAssets_ParallelCallsTwoClients_EachCoverEveryAssetOnce()
    {
        var ids = Ids(200);
        _source.Ids = ids;

        var tasks = Enumerable.Range(0, 80)
            .Select(i => i % 2 == 0 ? "a" : "b")
            .Select(client => (client, task: Task.Run(() => _pool.GetAssets(5, client))))
            .ToList();
        await Task.WhenAll(tasks.Select(t => t.task));

        foreach (var client in new[] { "a", "b" })
        {
            var dealt = tasks.Where(t => t.client == client).SelectMany(t => t.task.Result).Select(a => a.Id).ToList();
            Assert.That(dealt, Has.Count.EqualTo(200), client);
            Assert.That(dealt, Is.Unique, client);
            Assert.That(dealt, Is.EquivalentTo(ids), client);
        }

        Assert.That(_pool.DeckCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetAssets_ClientDecks_ShareOneListing()
    {
        _source.Ids = Ids(50);

        await Draw(5, "a");
        await Draw(5, "b");

        // The member set is held once, by the listing, not copied into each deck.
        Assert.That(_pool.ListingOf("a"), Is.SameAs(_source.Listing));
        Assert.That(_pool.ListingOf("b"), Is.SameAs(_source.Listing));
    }

    [Test]
    public async Task GetAssets_PastRateLimit_NewClientsShareDeckUntilIntervalPasses()
    {
        var ids = Ids(20);
        _source.Ids = ids;

        for (var i = 0; i < ShuffleDeckPool.RateLimitedAbove; i++)
        {
            await Draw(5, $"c{i}");
        }

        Assert.That(_pool.DeckCount, Is.EqualTo(ShuffleDeckPool.RateLimitedAbove));

        // Within the interval, new clients get no deck of their own; they share one.
        var shared = new List<Guid>();
        shared.AddRange(await Draw(5, "x"));
        shared.AddRange(await Draw(5, "y"));
        _time.Now += ShuffleDeckPool.NewDeckInterval - TimeSpan.FromSeconds(1);
        shared.AddRange(await Draw(5, "z"));
        Assert.That(_pool.DeckCount, Is.EqualTo(ShuffleDeckPool.RateLimitedAbove));
        Assert.That(_pool.ListingOf("x"), Is.Null);
        Assert.That(shared, Is.Unique, "the shared deck deals one cycle across those clients");

        _time.Now += TimeSpan.FromSeconds(1);
        await Draw(5, "x");
        Assert.That(_pool.DeckCount, Is.EqualTo(ShuffleDeckPool.RateLimitedAbove + 1));
        Assert.That(_pool.ListingOf("x"), Is.Not.Null);

        // Only one new deck per interval.
        await Draw(5, "y");
        Assert.That(_pool.ListingOf("y"), Is.Null);
    }

    [Test]
    public async Task GetAssets_AtCapWithAllDecksRecentlyUsed_EvictsNone()
    {
        var ids = Ids(40);
        _source.Ids = ids;

        for (var i = 0; i < ShuffleDeckPool.MaxDecks; i++)
        {
            _time.Now += ShuffleDeckPool.NewDeckInterval;
            await Draw(5, $"c{i}");
        }

        // Every deck has been used within the protected window.
        _time.Now += ShuffleDeckPool.NewDeckInterval;
        await Draw(5, "c0");
        _time.Now += TimeSpan.FromSeconds(1);
        for (var i = 1; i < ShuffleDeckPool.MaxDecks; i++)
        {
            await Draw(5, $"c{i}");
        }

        // c0, the least recently used, was used 9:59 ago.
        _time.Now += ShuffleDeckPool.ProtectedFor - TimeSpan.FromSeconds(2);
        await Draw(5, "newcomer");
        Assert.That(_pool.DeckCount, Is.EqualTo(ShuffleDeckPool.MaxDecks));
        Assert.That(_pool.ListingOf("newcomer"), Is.Null, "the newcomer is sent to the shared deck");
        Assert.That(_pool.ListingOf("c0"), Is.Not.Null);

        // Once the least recently used deck is past the window, it makes room.
        _time.Now += TimeSpan.FromSeconds(1);
        await Draw(5, "newcomer");
        Assert.That(_pool.DeckCount, Is.EqualTo(ShuffleDeckPool.MaxDecks));
        Assert.That(_pool.ListingOf("newcomer"), Is.Not.Null);
        Assert.That(_pool.ListingOf("c0"), Is.Null, "c0 was the least recently used");
        Assert.That(_pool.ListingOf("c1"), Is.Not.Null);
    }

    [Test]
    public async Task GetAssets_RetryIds_GoBackToDeckAndAreDealtLaterInCycle()
    {
        var ids = Ids(20);
        _source.Ids = ids;
        _source.Retry = (call, requested) => call == 0 ? requested.Take(2) : [];

        var first = (await _pool.GetAssets(5, "a")).Select(a => a.Id).ToList();
        var retried = _source.LoadRequests[0].Take(2).ToList();
        Assert.That(first, Has.Count.EqualTo(3));
        Assert.That(first, Does.Not.Contain(retried[0]).And.Not.Contain(retried[1]));

        // Without returning them, they would be skipped until the next cycle.
        var rest = await Draw(17, "a");
        Assert.That(first.Concat(rest), Is.EquivalentTo(ids));
    }

    [Test]
    public async Task GetAssets_RoundWithPassingFailure_DealsNoFurtherRounds()
    {
        var ids = Ids(100);
        _source.Ids = ids;
        // Immich is down: every load fails for a passing reason.
        _source.Retry = (_, requested) => requested;

        var assets = await _pool.GetAssets(5, "a");

        Assert.That(assets, Is.Empty);
        Assert.That(_source.LoadAssetsByIdsCalls, Is.EqualTo(1));
        Assert.That(_source.LoadRequests[0], Has.Count.EqualTo(5));
    }

    [Test]
    public async Task GetAssets_CancelledWhileLoading_DealtIdsGoBackToDeck()
    {
        var ids = Ids(20);
        _source.Ids = ids;
        _source.CancelOnCall = 0;

        Assert.That(async () => await _pool.GetAssets(5, "a"), Throws.InstanceOf<OperationCanceledException>());

        var all = await Draw(5, "a", calls: 4);
        Assert.That(all, Is.EquivalentTo(ids));
    }

    [Test]
    public async Task GetAssets_PassesCallerTokenToSource()
    {
        _source.Ids = Ids(20);
        using var cts = new CancellationTokenSource();

        await _pool.GetAssets(5, "a", cts.Token);

        Assert.That(_source.Tokens, Is.All.EqualTo(cts.Token));
    }
}
