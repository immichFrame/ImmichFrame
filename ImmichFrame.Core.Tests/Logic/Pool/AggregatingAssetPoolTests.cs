using Moq;
using NUnit.Framework;
using ImmichFrame.Core.Api; // For AssetResponseDto
using ImmichFrame.Core.Logic.Pool; // For AggregatingAssetPool and IAssetPool (non-generic)

namespace ImmichFrame.Core.Tests.Logic.Pool
{
    [TestFixture]
    public class AggregatingAssetPoolTests
    {
        private Mock<IAssetPool> _mockPool1;
        private Mock<IAssetPool> _mockPool2;
        private MultiAssetPool _aggregatingPool;
        private List<IAssetPool> _assetPools;

        [SetUp]
        public void Setup()
        {
            _mockPool1 = new Mock<IAssetPool>();
            _mockPool2 = new Mock<IAssetPool>();
            _assetPools = new List<IAssetPool>();
            // AggregatingAssetPool takes IEnumerable<IAssetPool> in constructor
        }

        private AssetResponseDto CreateAsset(string id) => new AssetResponseDto { Id = FixtureHelpers.GuidFor(id), OriginalPath = $"/path/{id}.jpg", Type = AssetTypeEnum.IMAGE, ExifInfo = new ExifResponseDto() };

        // MultiAssetPool asks each source once per batch for all of its slots, so mocks answer any size.
        private static void SetupQueuePool(Mock<IAssetPool> pool, Queue<AssetResponseDto> queue, Func<long>? count = null)
        {
            pool.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => count != null ? count() : queue.Count);
            pool.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int n, string _, CancellationToken _) =>
                {
                    var taken = new List<AssetResponseDto>();
                    while (taken.Count < n && queue.TryDequeue(out var asset))
                    {
                        taken.Add(asset);
                    }

                    return taken;
                });
        }

        private async Task<List<AssetResponseDto>> DrainAsync(int batchSize)
        {
            var collected = new List<AssetResponseDto>();
            for (var i = 0; i < 20 && await _aggregatingPool.GetAssetCount(CancellationToken.None) > 0; i++)
            {
                collected.AddRange(await _aggregatingPool.GetAssets(batchSize, ct: CancellationToken.None));
            }

            return collected;
        }

        [Test]
        public async Task GetAssetCount_NoPools_ReturnsZero()
        {
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool
            Assert.That(await _aggregatingPool.GetAssetCount(CancellationToken.None), Is.EqualTo(0));
        }

        [Test]
        public async Task GetAssetCount_OnePool_ReturnsCorrectCount()
        {
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(5L);
            _assetPools.Add(_mockPool1.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool
            Assert.That(await _aggregatingPool.GetAssetCount(CancellationToken.None), Is.EqualTo(5));
        }

        [Test]
        public async Task GetAssetCount_MultiplePools_ReturnsSumOfCounts()
        {
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(5L);
            _mockPool2.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(10L);
            _assetPools.Add(_mockPool1.Object);
            _assetPools.Add(_mockPool2.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool
            Assert.That(await _aggregatingPool.GetAssetCount(CancellationToken.None), Is.EqualTo(15));
        }

        [Test]
        public async Task GetAssets_RequestZeroAssets_ReturnsEmptyCollection()
        {
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool
            var result = await _aggregatingPool.GetAssets(0, ct: CancellationToken.None);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAssets_TotalLessThanRequested_ReturnsAllAvailableAssets()
        {
            var asset1 = CreateAsset("a1");
            var asset2 = CreateAsset("a2");
            var pool1AvailableAssets = new Queue<AssetResponseDto>(new List<AssetResponseDto> { asset1, asset2 });
            var allAssetsFromPool1 = new List<AssetResponseDto> { asset1, asset2 };

            SetupQueuePool(_mockPool1, pool1AvailableAssets);

            _assetPools.Add(_mockPool1.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            var result = (await _aggregatingPool.GetAssets(5, ct: CancellationToken.None)).ToList();
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(x => allAssetsFromPool1.Contains(x)), Is.True);
            Assert.That(allAssetsFromPool1.All(x => result.Contains(x)), Is.True);
        }

        [Test]
        public async Task GetAssets_TotalMoreThanRequested_AggregatesAssetsFromPools()
        {
            var assetP1A1 = CreateAsset("p1a1");
            var assetP1A2 = CreateAsset("p1a2");
            var pool1Queue = new Queue<AssetResponseDto>(new[] { assetP1A1, assetP1A2 });
            SetupQueuePool(_mockPool1, pool1Queue);

            var assetP2A1 = CreateAsset("p2a1");
            var pool2Queue = new Queue<AssetResponseDto>(new[] { assetP2A1 });
            SetupQueuePool(_mockPool2, pool2Queue);

            _assetPools.Add(_mockPool1.Object);
            _assetPools.Add(_mockPool2.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            // Request batches of 3 until drained. Pool1 has 2, Pool2 has 1. Slots are drawn by weight, so a
            // single batch may be short when one source is asked for more than it holds.
            var result = await DrainAsync(3);
            Assert.That(result.Count, Is.EqualTo(3));
            // Check presence of all expected assets, order might vary based on the weighted draw
            Assert.That(result, Does.Contain(assetP1A1));
            Assert.That(result, Does.Contain(assetP1A2));
            Assert.That(result, Does.Contain(assetP2A1));
        }

        [Test]
        public async Task GetAssets_PoolReturnsFewerAssetsThanCountSuggests_HandlesGracefully()
        {
            var p1a1 = CreateAsset("p1a1");
            var pool1AvailableAssets = new Queue<AssetResponseDto>(new List<AssetResponseDto> { p1a1 });
            var originalPool1Assets = new List<AssetResponseDto> { p1a1 };

            // Pool1 reports 5 assets, but its queue only has 1; it reports 0 once emptied.
            SetupQueuePool(_mockPool1, pool1AvailableAssets, () => pool1AvailableAssets.Count > 0 ? 5L : 0L);

            var p2a1 = CreateAsset("p2a1");
            var pool2AvailableAssets = new Queue<AssetResponseDto>(new List<AssetResponseDto> { p2a1 });
            var originalPool2Assets = new List<AssetResponseDto> { p2a1 };
            SetupQueuePool(_mockPool2, pool2AvailableAssets);

            _assetPools.Add(_mockPool1.Object);
            _assetPools.Add(_mockPool2.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            // Request batches of 5. Actual available: 1 from pool1, 1 from pool2. Total 2.
            // A batch is short when a source returns fewer than its slots; draining collects both.
            var first = (await _aggregatingPool.GetAssets(5, ct: CancellationToken.None)).ToList();
            Assert.That(first.Count, Is.LessThanOrEqualTo(2));
            var result = first.Concat(await DrainAsync(5)).ToList();

            Assert.That(result.Count, Is.EqualTo(2));
            var expectedTotalAssets = originalPool1Assets.Concat(originalPool2Assets).ToList();
            Assert.That(result.All(x => expectedTotalAssets.Contains(x)), Is.True);
            Assert.That(expectedTotalAssets.All(x => result.Contains(x)), Is.True);
        }

        // GetNextAsset is protected in AggregatingAssetPool. We test its behavior via GetAssets(1, ...)
        [Test]
        public async Task GetNextAssetBehavior_RetrievesAllAssets()
        {
            var asset1 = CreateAsset("asset1");
            var asset2 = CreateAsset("asset2");
            var asset3 = CreateAsset("asset3");

            var q1 = new Queue<AssetResponseDto>(new[] { asset1, asset2 });
            SetupQueuePool(_mockPool1, q1);

            var q2 = new Queue<AssetResponseDto>(new[] { asset3 });
            SetupQueuePool(_mockPool2, q2);

            _assetPools.Add(_mockPool1.Object);
            _assetPools.Add(_mockPool2.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            var retrievedAssets = new List<AssetResponseDto>();
            AssetResponseDto currentAsset;
            while ((currentAsset = (await _aggregatingPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault()) != null)
            {
                retrievedAssets.Add(currentAsset);
            }

            Assert.That(retrievedAssets.Count, Is.EqualTo(3));
            Assert.That(retrievedAssets, Does.Contain(asset1));
            Assert.That(retrievedAssets, Does.Contain(asset2));
            Assert.That(retrievedAssets, Does.Contain(asset3));
            Assert.That((await _aggregatingPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault(), Is.Null); // All exhausted
        }

        [Test]
        public async Task GetNextAssetBehavior_NoAssets_ReturnsNull()
        {
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<AssetResponseDto>());
            _assetPools.Add(_mockPool1.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            Assert.That((await _aggregatingPool.GetAssets(1, ct: CancellationToken.None)).FirstOrDefault(), Is.Null);
            _mockPool1.Verify(p => p.GetAssets(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        }
    }
}