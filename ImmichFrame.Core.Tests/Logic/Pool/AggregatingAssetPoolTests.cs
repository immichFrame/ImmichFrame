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

        private AssetResponseDto CreateAsset(string id) => new AssetResponseDto { Id = id, OriginalPath = $"/path/{id}.jpg", Type = AssetTypeEnum.IMAGE, ExifInfo = new ExifResponseDto() };

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
            var result = await _aggregatingPool.GetAssets(0, CancellationToken.None);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAssets_TotalLessThanRequested_ReturnsAllAvailableAssets()
        {
            var asset1 = CreateAsset("a1");
            var asset2 = CreateAsset("a2");
            var pool1AvailableAssets = new Queue<AssetResponseDto>(new List<AssetResponseDto> { asset1, asset2 });
            var allAssetsFromPool1 = new List<AssetResponseDto> { asset1, asset2 };

            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => (long)pool1AvailableAssets.Count);
            _mockPool1.Setup(p => p.GetAssets(1, It.IsAny<CancellationToken>())) // AggregatingAssetPool.GetNextAsset calls GetAssets(1,...)
                .ReturnsAsync(() => pool1AvailableAssets.Any()
                    ? new List<AssetResponseDto> { pool1AvailableAssets.Dequeue() }
                    : new List<AssetResponseDto>());

            _assetPools.Add(_mockPool1.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            var result = (await _aggregatingPool.GetAssets(5, CancellationToken.None)).ToList();
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
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(() => (long)pool1Queue.Count);
            _mockPool1.Setup(p => p.GetAssets(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => pool1Queue.Any() ? new List<AssetResponseDto> { pool1Queue.Dequeue() } : new List<AssetResponseDto>());

            var assetP2A1 = CreateAsset("p2a1");
            var pool2Queue = new Queue<AssetResponseDto>(new[] { assetP2A1 });
            _mockPool2.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(() => (long)pool2Queue.Count);
            _mockPool2.Setup(p => p.GetAssets(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => pool2Queue.Any() ? new List<AssetResponseDto> { pool2Queue.Dequeue() } : new List<AssetResponseDto>());

            _assetPools.Add(_mockPool1.Object);
            _assetPools.Add(_mockPool2.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            // Request 3 assets. Pool1 has 2, Pool2 has 1.
            var result = (await _aggregatingPool.GetAssets(3, CancellationToken.None)).ToList();
            Assert.That(result.Count, Is.EqualTo(3));
            // Check presence of all expected assets, order might vary based on AggregatingAssetPool internal logic
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

            // Pool1 reports 5 assets, but its queue only has 1.
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(5L);
            _mockPool1.Setup(p => p.GetAssets(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (pool1AvailableAssets.Any())
                    {
                        _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(0L); // Update count after last asset
                        return new List<AssetResponseDto> { pool1AvailableAssets.Dequeue() };
                    }

                    return new List<AssetResponseDto>();
                });

            var p2a1 = CreateAsset("p2a1");
            var pool2AvailableAssets = new Queue<AssetResponseDto>(new List<AssetResponseDto> { p2a1 });
            var originalPool2Assets = new List<AssetResponseDto> { p2a1 };
            _mockPool2.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => (long)pool2AvailableAssets.Count);
            _mockPool2.Setup(p => p.GetAssets(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => pool2AvailableAssets.Any() ? new List<AssetResponseDto> { pool2AvailableAssets.Dequeue() } : new List<AssetResponseDto>());

            _assetPools.Add(_mockPool1.Object);
            _assetPools.Add(_mockPool2.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            // Request 5 assets. Actual available: 1 from pool1, 1 from pool2. Total 2.
            // AggregatingAssetPool.GetAssets calls GetNextAsset. GetNextAsset iterates pools sequentially.
            // It will get p1a1 from pool1. Then pool1 is exhausted (its GetAssets(1,..) will return empty).
            // Then it will get p2a1 from pool2. Then pool2 is exhausted.
            // The loop in AggregatingAssetPool.GetAssets should break when GetNextAsset returns null.
            var result = (await _aggregatingPool.GetAssets(5, CancellationToken.None)).ToList();

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
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(() => (long)q1.Count);
            _mockPool1.Setup(p => p.GetAssets(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => q1.Any() ? new List<AssetResponseDto> { q1.Dequeue() } : new List<AssetResponseDto>());

            var q2 = new Queue<AssetResponseDto>(new[] { asset3 });
            _mockPool2.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(() => (long)q2.Count);
            _mockPool2.Setup(p => p.GetAssets(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => q2.Any() ? new List<AssetResponseDto> { q2.Dequeue() } : new List<AssetResponseDto>());

            _assetPools.Add(_mockPool1.Object);
            _assetPools.Add(_mockPool2.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            var retrievedAssets = new List<AssetResponseDto>();
            AssetResponseDto currentAsset;
            while ((currentAsset = (await _aggregatingPool.GetAssets(1, CancellationToken.None)).FirstOrDefault()) != null)
            {
                retrievedAssets.Add(currentAsset);
            }

            Assert.That(retrievedAssets.Count, Is.EqualTo(3));
            Assert.That(retrievedAssets, Does.Contain(asset1));
            Assert.That(retrievedAssets, Does.Contain(asset2));
            Assert.That(retrievedAssets, Does.Contain(asset3));
            Assert.That((await _aggregatingPool.GetAssets(1, CancellationToken.None)).FirstOrDefault(), Is.Null); // All exhausted
        }

        [Test]
        public async Task GetNextAssetBehavior_NoAssets_ReturnsNull()
        {
            _mockPool1.Setup(p => p.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockPool1.Setup(p => p.GetAssets(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AssetResponseDto>());
            _assetPools.Add(_mockPool1.Object);
            _aggregatingPool = new MultiAssetPool(_assetPools); // Use MultiAssetPool

            Assert.That((await _aggregatingPool.GetAssets(1, CancellationToken.None)).FirstOrDefault(), Is.Null);
        }
    }
}