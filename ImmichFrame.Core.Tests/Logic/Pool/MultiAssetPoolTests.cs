using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Logic.Pool;

namespace ImmichFrame.Core.Tests.Logic.Pool
{
    [TestFixture]
    public class MultiAssetPoolTests
    {
        private Mock<IAssetPool> _mockPool1;
        private Mock<IAssetPool> _mockPool2;
        private Mock<IAssetPool> _mockPool3;
        private MultiAssetPool _multiPool;

        [SetUp]
        public void Setup()
        {
            _mockPool1 = new Mock<IAssetPool>();
            _mockPool2 = new Mock<IAssetPool>();
            _mockPool3 = new Mock<IAssetPool>();
        }

        private AssetResponseDto CreateAsset(string id) => new AssetResponseDto { Id = FixtureHelpers.GuidFor(id), OriginalPath = $"/path/{id}.jpg", Type = AssetTypeEnum.IMAGE, ExifInfo = new ExifResponseDto() };

        private static IEnumerable<AssetResponseDto> Dequeue(Queue<AssetResponseDto> queue, int n)
        {
            var taken = new List<AssetResponseDto>();
            while (taken.Count < n && queue.TryDequeue(out var asset))
            {
                taken.Add(asset);
            }

            return taken;
        }

        /// <summary>
        /// Sets the pool up to report the queue length as its count (unless <paramref name="count"/> is given)
        /// and to answer a batch request for n assets with up to n assets dequeued from the queue.
        /// </summary>
        private static void SetupQueuePool(Mock<IAssetPool> pool, Queue<AssetResponseDto> queue, Func<long>? count = null)
        {
            pool.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => count != null ? count() : queue.Count);
            pool.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int n, string _, CancellationToken _) => Dequeue(queue, n));
        }

        /// <summary>
        /// Sets the pool up with a fixed count, answering every batch request with n copies of
        /// <paramref name="marker"/> and recording the requested size.
        /// </summary>
        private static void SetupEndlessPool(Mock<IAssetPool> pool, long count, AssetResponseDto marker, List<int> requestedSizes)
        {
            pool.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(count);
            pool.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int n, string _, CancellationToken _) =>
                {
                    requestedSizes.Add(n);
                    return Enumerable.Repeat(marker, n).ToList();
                });
        }

        [Test]
        public async Task GetAssetCount_NoPools_ReturnsZero()
        {
            _multiPool = new MultiAssetPool(new List<IAssetPool>());
            Assert.That(await _multiPool.GetAssetCount(CancellationToken.None), Is.EqualTo(0));
        }

        [Test]
        public async Task GetAssetCount_OnePool_ReturnsCorrectCount()
        {
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(5L);
            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object });
            Assert.That(await _multiPool.GetAssetCount(CancellationToken.None), Is.EqualTo(5L));
        }

        [Test]
        public async Task GetAssetCount_MultiplePools_ReturnsSumOfCounts()
        {
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(5L);
            _mockPool2.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(10L);
            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });
            Assert.That(await _multiPool.GetAssetCount(CancellationToken.None), Is.EqualTo(15L));
        }

        [Test]
        public async Task GetAssets_RequestZeroAssets_ReturnsEmptyCollection()
        {
            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object });
            var result = await _multiPool.GetAssets(0, ct: CancellationToken.None);
            Assert.That(result, Is.Empty);
            _mockPool1.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public async Task GetAssets_TotalLessThanRequested_ReturnsAllAvailableAssets()
        {
            var asset1 = CreateAsset("a1");
            var asset2 = CreateAsset("a2");
            var allAssetsFromPool1 = new List<AssetResponseDto> { asset1, asset2 };
            SetupQueuePool(_mockPool1, new Queue<AssetResponseDto>(allAssetsFromPool1));

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object });

            var result = (await _multiPool.GetAssets(5, ct: CancellationToken.None)).ToList();
            Assert.That(result, Is.EquivalentTo(allAssetsFromPool1));
            _mockPool1.Verify(p => p.GetAssets(5, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task GetAssets_TotalMoreThanRequested_AggregatesAssets()
        {
            var pool1Assets = Enumerable.Range(0, 100).Select(i => CreateAsset($"p1a{i}")).ToList();
            var pool2Assets = Enumerable.Range(0, 100).Select(i => CreateAsset($"p2a{i}")).ToList();
            SetupQueuePool(_mockPool1, new Queue<AssetResponseDto>(pool1Assets));
            SetupQueuePool(_mockPool2, new Queue<AssetResponseDto>(pool2Assets));

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });

            // Each pool can fill every slot, so the batch is full; with equal counts both pools contribute.
            var result = (await _multiPool.GetAssets(50, ct: CancellationToken.None)).ToList();
            Assert.That(result, Has.Count.EqualTo(50));
            Assert.That(result, Is.Unique);
            Assert.That(result.Any(pool1Assets.Contains), Is.True);
            Assert.That(result.Any(pool2Assets.Contains), Is.True);
        }

        [Test]
        public async Task GetAssets_PoolReturnsFewerAssetsThanCountSuggests_HandlesGracefully()
        {
            var p1a1 = CreateAsset("p1a1");
            var p1a2 = CreateAsset("p1a2");
            var pool1Queue = new Queue<AssetResponseDto>(new[] { p1a1, p1a2 });
            // Pool1 claims 5 assets while it holds only 2, until it has been emptied.
            SetupQueuePool(_mockPool1, pool1Queue, () => pool1Queue.Count > 0 ? 5L : 0L);

            var p2a1 = CreateAsset("p2a1");
            SetupQueuePool(_mockPool2, new Queue<AssetResponseDto>(new[] { p2a1 }));

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });

            // A source that returns fewer than asked leaves its slots empty: the batch is short, not padded.
            var first = (await _multiPool.GetAssets(5, ct: CancellationToken.None)).ToList();
            Assert.That(first.Count, Is.LessThanOrEqualTo(3));
            Assert.That(first, Has.None.Null);

            var collected = new List<AssetResponseDto>(first);
            for (var i = 0; i < 20 && await _multiPool.GetAssetCount(CancellationToken.None) > 0; i++)
            {
                collected.AddRange(await _multiPool.GetAssets(5, ct: CancellationToken.None));
            }

            Assert.That(collected, Is.EquivalentTo(new[] { p1a1, p1a2, p2a1 }));
        }

        [Test]
        public async Task GetAssets_DifferentAssetCounts_RetrievesAssetsFromPools()
        {
            var originalPool1Assets = new List<AssetResponseDto> { CreateAsset("p1a1"), CreateAsset("p1a2"), CreateAsset("p1a3") };
            var originalPool2Assets = new List<AssetResponseDto> { CreateAsset("p2a1") };
            SetupQueuePool(_mockPool1, new Queue<AssetResponseDto>(originalPool1Assets));
            SetupQueuePool(_mockPool2, new Queue<AssetResponseDto>(originalPool2Assets));

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });

            var retrievedAssets = new List<AssetResponseDto>();
            for (int i = 0; i < 5; i++)
            {
                var assetResult = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
                if (assetResult != null)
                {
                    retrievedAssets.Add(assetResult);
                }
                else if (retrievedAssets.Count >= 4)
                {
                    break;
                }
            }

            Assert.That(retrievedAssets.Count, Is.EqualTo(4));
            Assert.That(retrievedAssets.Count(a => originalPool1Assets.Contains(a)), Is.EqualTo(3));
            Assert.That(retrievedAssets.Count(a => originalPool2Assets.Contains(a)), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAssets_PoolWithZeroCount_IsNotCalledForAssets()
        {
            var asset1 = CreateAsset("p1a1");
            SetupQueuePool(_mockPool1, new Queue<AssetResponseDto>(new[] { asset1 }));

            _mockPool2.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockPool2.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<AssetResponseDto>());

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });
            var result = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).ToList();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First(), Is.SameAs(asset1));
            _mockPool2.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public async Task GetNextAssetBehavior_NoAssetsInAnyPool_ReturnsNull()
        {
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockPool2.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });

            var result = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
            Assert.That(result, Is.Null);

            var batch = await _multiPool.GetAssets(25, ct: CancellationToken.None);
            Assert.That(batch, Is.Empty);
            _mockPool1.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
            _mockPool2.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public async Task GetNextAssetBehavior_RetrievesAllAssets()
        {
            var assetP1A1 = CreateAsset("P1A1");
            var assetP2A1 = CreateAsset("P2A1");
            SetupQueuePool(_mockPool1, new Queue<AssetResponseDto>(new[] { assetP1A1 }));
            SetupQueuePool(_mockPool2, new Queue<AssetResponseDto>(new[] { assetP2A1 }));

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });

            var results = new HashSet<AssetResponseDto>();
            var asset1 = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
            if (asset1 != null) results.Add(asset1);

            var asset2 = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
            if (asset2 != null) results.Add(asset2);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results, Does.Contain(assetP1A1));
            Assert.That(results, Does.Contain(assetP2A1));

            var asset3 = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
            Assert.That(asset3, Is.Null);
        }

        [Test]
        public async Task GetNextAssetBehavior_PoolWithZeroCount_IsNotCalled()
        {
            var asset1 = CreateAsset("p1a1");
            SetupQueuePool(_mockPool1, new Queue<AssetResponseDto>(new[] { asset1 }));

            _mockPool2.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockPool2.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<AssetResponseDto>());

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });

            var result = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
            Assert.That(result, Is.SameAs(asset1));

            _mockPool1.Verify(p => p.GetAssets(1, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
            _mockPool2.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());

            var nextResult = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
            Assert.That(nextResult, Is.Null, "Pool1 exhausted, Pool2 zero count, should be null");
        }

        [Test]
        public async Task GetNextAssetBehavior_PoolIsExhausted_SwitchesToNextAvailablePool()
        {
            var assetP2A1 = CreateAsset("P2A1");
            var assetP3A1 = CreateAsset("P3A1");

            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            SetupQueuePool(_mockPool2, new Queue<AssetResponseDto>(new[] { assetP2A1 }));
            SetupQueuePool(_mockPool3, new Queue<AssetResponseDto>(new[] { assetP3A1 }));

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object, _mockPool3.Object });

            var result1 = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
            Assert.That(result1, Is.EqualTo(assetP2A1).Or.EqualTo(assetP3A1));
            var result2 = (await _multiPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault();
            if (result1 == assetP2A1)
                Assert.That(result2, Is.SameAs(assetP3A1), "Should get asset from Pool 3 if Pool 2 was first");
            else
                Assert.That(result2, Is.SameAs(assetP2A1), "Should get asset from Pool 2 if Pool 3 was first");
        }

        [Test]
        public async Task GetAssets_Batch_CallsEachSourceOnce()
        {
            var sizes1 = new List<int>();
            var sizes2 = new List<int>();
            var sizes3 = new List<int>();
            SetupEndlessPool(_mockPool1, 1000, CreateAsset("p1"), sizes1);
            SetupEndlessPool(_mockPool2, 1000, CreateAsset("p2"), sizes2);
            SetupEndlessPool(_mockPool3, 0, CreateAsset("p3"), sizes3);

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object, _mockPool3.Object });

            var result = (await _multiPool.GetAssets(25, ct: CancellationToken.None)).ToList();

            Assert.That(result, Has.Count.EqualTo(25));
            _mockPool1.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
            _mockPool2.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
            _mockPool3.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
            Assert.That(sizes1.Sum() + sizes2.Sum() + sizes3.Sum(), Is.EqualTo(25));
            _mockPool1.Verify(p => p.GetAssetCount(It.IsAny<CancellationToken>()), Times.Once());
            _mockPool2.Verify(p => p.GetAssetCount(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task GetAssets_Batch_KeepsWeightingByCount()
        {
            var markerA = CreateAsset("A");
            var markerB = CreateAsset("B");
            var sizesA = new List<int>();
            var sizesB = new List<int>();
            SetupEndlessPool(_mockPool1, 900, markerA, sizesA);
            SetupEndlessPool(_mockPool2, 100, markerB, sizesB);

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object, _mockPool2.Object });

            var fromA = 0;
            var total = 0;
            for (var i = 0; i < 200; i++)
            {
                var batch = (await _multiPool.GetAssets(25, ct: CancellationToken.None)).ToList();
                total += batch.Count;
                fromA += batch.Count(a => ReferenceEquals(a, markerA));
            }

            Assert.That(total, Is.EqualTo(200 * 25));
            Assert.That(sizesA.Sum() + sizesB.Sum(), Is.EqualTo(200 * 25));
            var shareA = (double)fromA / total;
            Assert.That(shareA, Is.InRange(0.8, 0.97));
        }

        [Test]
        public async Task GetAssets_PassesCancellationTokenToSources()
        {
            using var cts = new CancellationTokenSource();
            var seenTokens = new List<CancellationToken>();
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(10L);
            _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int n, string _, CancellationToken token) =>
                {
                    seenTokens.Add(token);
                    return Enumerable.Range(0, n).Select(i => CreateAsset($"ct{i}")).ToList();
                });

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object });

            var result = await _multiPool.GetAssets(5, ct: cts.Token);

            Assert.That(result.Count(), Is.EqualTo(5));
            Assert.That(seenTokens, Is.EqualTo(new[] { cts.Token }));
            _mockPool1.Verify(p => p.GetAssetCount(cts.Token), Times.Once());
        }

        [Test]
        public async Task GetAssets_PassesClientIdToSources()
        {
            var seenClientIds = new List<string>();
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(10L);
            _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int n, string clientId, CancellationToken _) =>
                {
                    seenClientIds.Add(clientId);
                    return Enumerable.Range(0, n).Select(i => CreateAsset($"c{i}")).ToList();
                });

            _multiPool = new MultiAssetPool(new List<IAssetPool> { _mockPool1.Object });

            var result = await _multiPool.GetAssets(3, "frame-1", CancellationToken.None);

            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(seenClientIds, Is.EqualTo(new[] { "frame-1" }));
        }
    }
}
