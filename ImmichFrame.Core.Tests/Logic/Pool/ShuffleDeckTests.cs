using ImmichFrame.Core.Logic.Pool;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class ShuffleDeckTests
{
    private const int SeedCount = 50;

    private static IEnumerable<int> Seeds => Enumerable.Range(0, SeedCount);

    private static List<Guid> Ids(int count, string prefix = "id") =>
        Enumerable.Range(0, count).Select(i => FixtureHelpers.GuidFor($"{prefix}{i}")).ToList();

    private static ShuffleDeck NewDeck(int seed, IReadOnlyCollection<Guid> ids)
    {
        var deck = new ShuffleDeck(new Random(seed));
        deck.Sync(ids);
        return deck;
    }

    // Deals one card at a time, so no new cycle is started as long as count <= the cards left in the cycle.
    private static List<Guid> DealSingles(ShuffleDeck deck, int count)
    {
        var dealt = new List<Guid>(count);
        for (var i = 0; i < count; i++)
        {
            var batch = deck.Deal(1);
            Assert.That(batch, Has.Count.EqualTo(1));
            dealt.Add(batch[0]);
        }

        return dealt;
    }

    [Test]
    public void Deal_BatchLargerThanAlbum_ReturnsEveryMemberOnce()
    {
        var ids = Ids(10);
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            for (var call = 0; call < 5; call++)
            {
                var batch = deck.Deal(25);
                Assert.That(batch, Has.Count.EqualTo(10), $"seed {seed}, call {call}");
                Assert.That(batch, Is.EquivalentTo(ids), $"seed {seed}, call {call}");
            }
        }
    }

    [Test]
    public void Deal_BatchDividesAlbum_CoversAllThenAvoidsLastBatch()
    {
        var ids = Ids(100);
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var batches = Enumerable.Range(0, 5).Select(_ => deck.Deal(25)).ToList();

            Assert.That(batches.Take(4).SelectMany(b => b), Is.EquivalentTo(ids), $"seed {seed}");
            Assert.That(batches[4], Has.Count.EqualTo(25), $"seed {seed}");
            Assert.That(batches[4], Is.Unique, $"seed {seed}");
            Assert.That(batches[4].Intersect(batches[3]), Is.Empty, $"seed {seed}");
        }
    }

    [Test]
    public void Deal_BatchDoesNotDivideAlbum_JoinBatchHasNoRepeats()
    {
        var ids = Ids(110);
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var stream = new List<Guid>();
            var batches = new List<IReadOnlyList<Guid>>();
            for (var call = 0; call < 5; call++)
            {
                var batch = deck.Deal(25);
                Assert.That(batch, Has.Count.EqualTo(25), $"seed {seed}, call {call}");
                batches.Add(batch);
                stream.AddRange(batch);
            }

            // The first 110 dealt are one full cycle.
            Assert.That(stream.Take(110), Is.EquivalentTo(ids), $"seed {seed}");

            // The 5th batch holds the last 10 of the first cycle and the first 15 of the next.
            var fifth = batches[4];
            Assert.That(fifth, Is.Unique, $"seed {seed}");

            var newCycleCards = stream.Skip(110).Take(15).ToList();
            var lastDealtBeforeNewCycle = stream.Skip(110 - ShuffleDeck.RecentWindow).Take(ShuffleDeck.RecentWindow).ToList();
            Assert.That(newCycleCards.Intersect(lastDealtBeforeNewCycle), Is.Empty, $"seed {seed}");
        }
    }

    [Test]
    public void Deal_Sweep_CyclesArePermutationsWithoutRepeats(
        [Values(1, 2, 3, 7, 26, 60)] int albumSize,
        [Values(1, 7, 25)] int batchSize)
    {
        var ids = Ids(albumSize);
        var expectedBatch = Math.Min(batchSize, albumSize);

        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var stream = new List<Guid>();
            while (stream.Count < 3 * albumSize + batchSize)
            {
                var batch = deck.Deal(batchSize);
                Assert.That(batch, Has.Count.EqualTo(expectedBatch), $"seed {seed}");
                Assert.That(batch, Is.Unique, $"seed {seed}");
                stream.AddRange(batch);
            }

            for (var k = 0; (k + 1) * albumSize <= stream.Count; k++)
            {
                var cycle = stream.Skip(k * albumSize).Take(albumSize);
                Assert.That(cycle, Is.EquivalentTo(ids), $"seed {seed}, cycle {k}");
            }

            if (albumSize >= 2)
            {
                for (var i = 1; i < stream.Count; i++)
                {
                    Assert.That(stream[i], Is.Not.EqualTo(stream[i - 1]), $"seed {seed}, position {i}");
                }
            }
        }
    }

    [Test]
    public void Deal_SingleMember_AlwaysReturnsThatMember()
    {
        var id = FixtureHelpers.GuidFor("only");
        var deck = NewDeck(0, [id]);

        foreach (var n in new[] { 1, 2, 7, 25 })
        {
            Assert.That(deck.Deal(n), Is.EqualTo(new[] { id }));
        }
    }

    [Test]
    public void Deal_NonPositiveCount_ReturnsEmpty()
    {
        var deck = NewDeck(0, Ids(10));

        Assert.That(deck.Deal(0), Is.Empty);
        Assert.That(deck.Deal(-3), Is.Empty);
    }

    [Test]
    public void Deal_NoMembers_ReturnsEmpty()
    {
        var unsynced = new ShuffleDeck(new Random(0));
        Assert.That(unsynced.Deal(5), Is.Empty);
        Assert.That(unsynced.Count, Is.EqualTo(0));

        var syncedEmpty = NewDeck(0, []);
        Assert.That(syncedEmpty.Deal(5), Is.Empty);
    }

    [Test]
    public void Sync_EmptyMidCycle_DealsNothingUntilMembersReturn()
    {
        var ids = Ids(10);
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            deck.Deal(4);

            deck.Sync([]);
            Assert.That(deck.Count, Is.EqualTo(0));
            Assert.That(deck.Deal(5), Is.Empty, $"seed {seed}");

            var next = Ids(6, "next");
            deck.Sync(next);
            Assert.That(deck.Count, Is.EqualTo(6));
            Assert.That(deck.Deal(6), Is.EquivalentTo(next), $"seed {seed}");
        }
    }

    [Test]
    public void Sync_RemovingUndealtIds_RestOfCycleCoversRemainingOnce()
    {
        var ids = Ids(20);
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var before = deck.Deal(5);

            var removed = ids.Except(before).Take(3).ToList();
            var remaining = ids.Except(removed).ToList();
            deck.Sync(remaining);

            // 15 were left in the cycle; 3 of them are gone.
            var restOfCycle = DealSingles(deck, 12);
            Assert.That(restOfCycle, Is.EquivalentTo(remaining.Except(before)), $"seed {seed}");

            var later = DealSingles(deck, 3 * remaining.Count);
            Assert.That(later.Intersect(removed), Is.Empty, $"seed {seed}");
            for (var k = 0; k < 3; k++)
            {
                Assert.That(later.Skip(k * remaining.Count).Take(remaining.Count), Is.EquivalentTo(remaining), $"seed {seed}, cycle {k}");
            }
        }
    }

    [Test]
    public void Sync_RemovingDealtId_NoEffectThisCycleAbsentFromNext()
    {
        var ids = Ids(20);
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var before = deck.Deal(5);

            var removed = before[0];
            var remaining = ids.Where(id => id != removed).ToList();
            deck.Sync(remaining);

            var restOfCycle = DealSingles(deck, 15);
            Assert.That(restOfCycle, Is.EquivalentTo(ids.Except(before)), $"seed {seed}");

            var nextCycle = DealSingles(deck, remaining.Count);
            Assert.That(nextCycle, Is.EquivalentTo(remaining), $"seed {seed}");
        }
    }

    [Test]
    public void Sync_AddingIdsMidCycle_DealsThemBeforeCycleEnds()
    {
        var ids = Ids(20);
        var added = Ids(5, "added");
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var before = deck.Deal(8);

            var all = ids.Concat(added).ToList();
            deck.Sync(all);

            // 12 were left in the cycle, plus the 5 newcomers.
            var restOfCycle = DealSingles(deck, 17);
            Assert.That(restOfCycle, Is.Unique, $"seed {seed}");
            Assert.That(restOfCycle, Is.SupersetOf(added), $"seed {seed}");
            Assert.That(restOfCycle.Intersect(before), Is.Empty, $"seed {seed}");
            Assert.That(before.Concat(restOfCycle), Is.EquivalentTo(all), $"seed {seed}");

            var nextCycle = DealSingles(deck, all.Count);
            Assert.That(nextCycle, Is.EquivalentTo(all), $"seed {seed}");
        }
    }

    [Test]
    public void Sync_AddingAndRemovingAtOnce_AppliesBoth()
    {
        var ids = Ids(20);
        var added = Ids(3, "added");
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var before = deck.Deal(8);

            var removedUndealt = ids.Except(before).Take(2).ToList();
            var removedDealt = before[0];
            var removed = removedUndealt.Append(removedDealt).ToList();
            var members = ids.Except(removed).Concat(added).ToList();
            deck.Sync(members);
            Assert.That(deck.Count, Is.EqualTo(20));

            // 12 left - 2 removed + 3 added.
            var restOfCycle = DealSingles(deck, 13);
            Assert.That(restOfCycle, Is.EquivalentTo(ids.Except(before).Except(removedUndealt).Concat(added)), $"seed {seed}");

            var later = DealSingles(deck, 2 * members.Count);
            Assert.That(later.Take(members.Count), Is.EquivalentTo(members), $"seed {seed}");
            Assert.That(later.Skip(members.Count), Is.EquivalentTo(members), $"seed {seed}");
            Assert.That(later.Intersect(removed), Is.Empty, $"seed {seed}");
        }
    }

    // Newcomers synced after the cycle has been fully dealt join the (otherwise empty) remainder of
    // that cycle: they are dealt first, then a full new cycle of every member follows.
    [Test]
    public void Sync_AddingIdsWhenCycleFullyDealt_DealsNewcomersInNextCycleWithTheRest()
    {
        var ids = Ids(10);
        var added = Ids(5, "added");
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            Assert.That(deck.Deal(10), Is.EquivalentTo(ids), $"seed {seed}");

            var all = ids.Concat(added).ToList();
            deck.Sync(all);

            var nextCycle = DealSingles(deck, all.Count);
            Assert.That(nextCycle, Is.EquivalentTo(all), $"seed {seed}");
        }
    }

    [Test]
    public void Sync_FirstSync_Shuffles()
    {
        var ids = Ids(20);
        var firstCards = Seeds.Select(seed => NewDeck(seed, ids).Deal(1)[0]).ToList();

        Assert.That(firstCards.Any(card => card != ids[0]), Is.True);
        Assert.That(firstCards.Distinct().Count(), Is.GreaterThan(1));
    }

    [Test]
    public void Count_TracksMembers()
    {
        var deck = NewDeck(0, Ids(7));
        Assert.That(deck.Count, Is.EqualTo(7));

        deck.Sync(Ids(3));
        Assert.That(deck.Count, Is.EqualTo(3));
    }

    [Test]
    public void Sync_OlderListing_IsIgnored()
    {
        var older = new AssetIdList(Ids(10, "old"));
        var newer = new AssetIdList(Ids(10, "new"));
        var deck = new ShuffleDeck(new Random(1));

        Assert.That(deck.Sync(newer), Is.True);
        // A slow request that started before the refresh must not take the deck back.
        Assert.That(deck.Sync(older), Is.False);
        Assert.That(deck.Sync(newer), Is.False, "the same listing is not synced twice");
        Assert.That(deck.Version, Is.EqualTo(newer.Version));
        Assert.That(deck.Deal(10), Is.EquivalentTo(newer.Ids));
    }

    [Test]
    public void Version_BeforeFirstSync_IsMinusOne()
    {
        Assert.That(new ShuffleDeck(new Random(1)).Version, Is.EqualTo(-1));
    }

    [Test]
    public void Return_PutsUnshownCardsBackIntoCycle()
    {
        var ids = Ids(20);
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var first = deck.Deal(5);
            deck.Return(first.Take(2));

            var rest = DealSingles(deck, 17);
            Assert.That(first.Skip(2).Concat(rest), Is.EquivalentTo(ids), $"seed {seed}");
        }
    }

    [Test]
    public void Return_IgnoresNonMembersAndCardsStillUndealt()
    {
        var ids = Ids(10);
        var deck = NewDeck(3, ids);
        var first = deck.Deal(3);

        deck.Return([FixtureHelpers.GuidFor("stranger"), first[0], first[0]]);
        var undealt = deck.Deal(1).Concat(DealSingles(deck, 7)).ToList();
        deck.Return([undealt[0]]);
        deck.Return([undealt[0]]);

        Assert.That(undealt, Has.Count.EqualTo(8));
        Assert.That(undealt, Is.EquivalentTo(ids.Except(first.Skip(1))));
        Assert.That(DealSingles(deck, 1), Is.EqualTo(new[] { undealt[0] }));
    }

    [Test]
    public void Sync_AddingIdsAfterCycleJoin_KeepsRecentCardsOutOfItsStart()
    {
        var ids = Ids(60);
        var newcomers = Ids(30, "new");
        foreach (var seed in Seeds)
        {
            var deck = NewDeck(seed, ids);
            var lastCycle = DealSingles(deck, 60);
            DealSingles(deck, 1);

            deck.Sync(ids.Concat(newcomers).ToList());
            var next = DealSingles(deck, ShuffleDeck.RecentWindow - 1);

            Assert.That(next.Intersect(lastCycle.TakeLast(ShuffleDeck.RecentWindow)), Is.Empty, $"seed {seed}");
        }
    }

    [Test]
    public void Sync_ManyNewcomersIntoLargeDeck_TakesLinearTime()
    {
        var ids = Enumerable.Range(0, 100_000).Select(_ => Guid.NewGuid()).ToList();
        var deck = NewDeck(1, ids);
        deck.Deal(1);
        var grown = ids.Concat(Enumerable.Range(0, 50_000).Select(_ => Guid.NewGuid())).ToList();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        deck.Sync(grown);
        watch.Stop();

        // Inserting each newcomer by shifting the list takes tens of seconds here.
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
        Assert.That(deck.Deal(grown.Count - 1), Is.Unique);
    }

    [Test]
    public void Return_ManyCallsOnLargeDeck_CostsNothingPerDeckSize()
    {
        var ids = Enumerable.Range(0, 200_000).Select(_ => Guid.NewGuid()).ToList();
        var deck = NewDeck(1, ids);
        deck.Deal(1);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 2_000; i++)
        {
            deck.Return(deck.Deal(25));
        }

        watch.Stop();

        // A scan and a shift of the whole deck per returned card takes many seconds here.
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
        Assert.That(deck.Deal(ids.Count - 1), Is.Unique);
    }
}
