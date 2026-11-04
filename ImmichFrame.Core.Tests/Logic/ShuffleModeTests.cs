using ImmichFrame.Core.Api;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Logic.Pool;
using ImmichFrame.Core.Models;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic;

[TestFixture]
public class ShuffleModeTests
{
    private Mock<IDealableAssetPool> _source;

    [SetUp]
    public void Setup()
    {
        _source = new Mock<IDealableAssetPool>();
    }

    [Test]
    public void ApplyShuffleMode_Random_ReturnsTheSameInstance()
    {
        var pool = PooledImmichFrameLogic.ApplyShuffleMode(_source.Object, ShuffleMode.Random);

        Assert.That(pool, Is.SameAs(_source.Object));
    }

    [Test]
    public void ApplyShuffleMode_Exhaustive_WrapsInShuffleDeckPool()
    {
        var pool = PooledImmichFrameLogic.ApplyShuffleMode(_source.Object, ShuffleMode.Exhaustive);

        Assert.That(pool, Is.InstanceOf<ShuffleDeckPool>());
    }

    [Test]
    public async Task ApplyShuffleMode_Exhaustive_DealsFromTheSourceIds()
    {
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
        _source.Setup(s => s.GetAllAssetIds(It.IsAny<CancellationToken>())).ReturnsAsync(new AssetIdList(ids));
        _source.Setup(s => s.LoadAssetsByIds(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> requested, CancellationToken _) =>
                AssetLoadResult.Of(requested.Select(id => new AssetResponseDto { Id = id, Type = AssetTypeEnum.IMAGE }).ToList()));

        var pool = PooledImmichFrameLogic.ApplyShuffleMode(_source.Object, ShuffleMode.Exhaustive);
        var assets = (await pool.GetAssets(3, "frame-1")).ToList();

        Assert.That(assets.Select(a => a.Id), Is.EquivalentTo(ids));
        _source.Verify(s => s.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ShuffleMode_DefaultsToRandom()
    {
        Assert.That(default(ShuffleMode), Is.EqualTo(ShuffleMode.Random));
    }
}
