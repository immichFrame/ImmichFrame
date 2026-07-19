using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class ShuffleBagAssetPoolTests
{
    private static List<AssetResponseDto> Sample(int n) =>
        Enumerable.Range(0, n)
            .Select(i => new AssetResponseDto { Id = $"asset{i}", Type = AssetTypeEnum.IMAGE })
            .ToList();

    private static IAssetPool InnerReturning(IReadOnlyList<AssetResponseDto> assets)
    {
        var mock = new Mock<IAssetPool>();
        mock.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(assets.Count);
        mock.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => assets);
        return mock.Object;
    }

    // Like InnerReturning, but honors the requested page size the way Immich's random
    // search does (returns at most `n` assets) — needed to exercise the MaxBagSize cap.
    private static IAssetPool InnerHonoringSize(IReadOnlyList<AssetResponseDto> assets)
    {
        var mock = new Mock<IAssetPool>();
        mock.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(assets.Count);
        mock.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int n, CancellationToken _) => assets.Take(n).ToList());
        return mock.Object;
    }

    [Test]
    public async Task ShowsEachAssetOnceBeforeRepeating()
    {
        var assets = Sample(10);
        var pool = new ShuffleBagAssetPool(InnerReturning(assets));

        var cycle = (await pool.GetAssets(10)).ToList();

        Assert.That(cycle, Has.Count.EqualTo(10));
        Assert.That(cycle.Select(a => a.Id).Distinct().Count(), Is.EqualTo(10), "no repeats within a cycle");
        Assert.That(cycle.Select(a => a.Id), Is.EquivalentTo(assets.Select(a => a.Id)), "every asset shown once");
    }

    [Test]
    public async Task ReshufflesAndCoversFullSetEachCycle()
    {
        var assets = Sample(10);
        var pool = new ShuffleBagAssetPool(InnerReturning(assets));

        var first = (await pool.GetAssets(10)).Select(a => a.Id).ToList();
        var second = (await pool.GetAssets(10)).Select(a => a.Id).ToList();

        Assert.That(first, Is.EquivalentTo(assets.Select(a => a.Id)));
        Assert.That(second, Is.EquivalentTo(assets.Select(a => a.Id)), "no starvation: full set served again next cycle");
    }

    [Test]
    public async Task DedupesAssetsFromInnerPool()
    {
        var withDupes = new List<AssetResponseDto>
        {
            new() { Id = "a", Type = AssetTypeEnum.IMAGE },
            new() { Id = "a", Type = AssetTypeEnum.IMAGE },
            new() { Id = "b", Type = AssetTypeEnum.IMAGE },
        };
        var pool = new ShuffleBagAssetPool(InnerReturning(withDupes));

        var cycle = (await pool.GetAssets(2)).Select(a => a.Id).ToList();

        Assert.That(cycle, Has.Count.EqualTo(2));
        Assert.That(cycle.Distinct().Count(), Is.EqualTo(cycle.Count), "no duplicate ids served within a cycle");
    }

    [Test]
    public async Task EmptyPoolYieldsNothing()
    {
        var pool = new ShuffleBagAssetPool(InnerReturning(new List<AssetResponseDto>()));

        var result = await pool.GetAssets(5);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task CapsBagAtMaxBagSizeForLargeLibraries()
    {
        // Mirrors ShuffleBagAssetPool.MaxBagSize (Immich's random-search page cap).
        // Above this size each cycle is a rolling no-repeat window, not the whole library.
        const int maxBagSize = 1000;
        var assets = Sample(maxBagSize + 10);
        var pool = new ShuffleBagAssetPool(InnerHonoringSize(assets));

        var cycle = (await pool.GetAssets(maxBagSize)).Select(a => a.Id).ToList();

        Assert.That(cycle, Has.Count.EqualTo(maxBagSize), "bag is capped at MaxBagSize");
        Assert.That(cycle.Distinct().Count(), Is.EqualTo(maxBagSize), "no repeats within the capped window");
        Assert.That(cycle, Is.SubsetOf(assets.Select(a => a.Id)), "all drawn from the inner set");
    }

    [Test]
    public async Task GetAssetCountDelegatesToInner()
    {
        var pool = new ShuffleBagAssetPool(InnerReturning(Sample(7)));

        Assert.That(await pool.GetAssetCount(), Is.EqualTo(7));
    }
}
