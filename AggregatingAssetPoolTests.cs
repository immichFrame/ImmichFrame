using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Linq;

// Assuming the classes are in a namespace, adjust if necessary
// namespace YourNamespace.Tests;

[TestFixture]
public class AggregatingAssetPoolTests
{
    private Mock<IAssetPool<object>> _mockPool1;
    private Mock<IAssetPool<object>> _mockPool2;
    private List<IAssetPool<object>> _assetPools;
    private AggregatingAssetPool<object> _aggregatingPool;

    [SetUp]
    public void Setup()
    {
        _mockPool1 = new Mock<IAssetPool<object>>();
        _mockPool2 = new Mock<IAssetPool<object>>();
        _assetPools = new List<IAssetPool<object>>();
        // AggregatingAssetPool needs to be defined or imported.
        // For now, I'll assume it exists and can be instantiated.
        // If not, this will be a placeholder for its actual instantiation.
        // _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);
    }

    // Placeholder for AggregatingAssetPool and IAssetPool if not defined elsewhere
    // This is just for the structure and will be replaced by actual implementation details
    public interface IAssetPool<T>
    {
        int GetAssetCount();
        IEnumerable<T> GetAssets(int count);
        T GetNextAsset();
    }

    public class AggregatingAssetPool<T>
    {
        private readonly List<IAssetPool<T>> _pools;
        private int _currentPoolIndex = 0;

        public AggregatingAssetPool(List<IAssetPool<T>> pools)
        {
            _pools = pools;
        }

        public int GetAssetCount()
        {
            return _pools.Sum(p => p.GetAssetCount());
        }

        public IEnumerable<T> GetAssets(int count)
        {
            if (count == 0) return Enumerable.Empty<T>();

            var assets = new List<T>();
            var remainingAssetsToFetch = count;

            foreach (var pool in _pools)
            {
                if (remainingAssetsToFetch == 0) break;

                var assetsFromPool = pool.GetAssets(remainingAssetsToFetch);
                if (assetsFromPool != null)
                {
                    assets.AddRange(assetsFromPool);
                    remainingAssetsToFetch -= assetsFromPool.Count();
                }
            }
            return assets;
        }

        public T GetNextAsset()
        {
            if (_pools == null || _pools.Count == 0)
            {
                return default(T); // Or throw exception
            }

            while (_currentPoolIndex < _pools.Count)
            {
                var asset = _pools[_currentPoolIndex].GetNextAsset();
                if (asset != null)
                {
                    return asset;
                }
                _currentPoolIndex++;
            }
            return default(T); // Or throw exception if no more assets
        }
    }

    [Test]
    public void GetAssetCount_NoPools_ReturnsZero()
    {
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);
        Assert.AreEqual(0, _aggregatingPool.GetAssetCount());
    }

    [Test]
    public void GetAssetCount_OnePool_ReturnsCorrectCount()
    {
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(5);
        _assetPools.Add(_mockPool1.Object);
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);
        Assert.AreEqual(5, _aggregatingPool.GetAssetCount());
    }

    [Test]
    public void GetAssetCount_MultiplePools_ReturnsSumOfCounts()
    {
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(5);
        _mockPool2.Setup(p => p.GetAssetCount()).Returns(10);
        _assetPools.Add(_mockPool1.Object);
        _assetPools.Add(_mockPool2.Object);
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);
        Assert.AreEqual(15, _aggregatingPool.GetAssetCount());
    }

    [Test]
    public void GetAssets_RequestZeroAssets_ReturnsEmptyCollection()
    {
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);
        var result = _aggregatingPool.GetAssets(0);
        Assert.IsEmpty(result);
    }

    [Test]
    public void GetAssets_TotalLessThanRequested_ReturnsAllAvailableAssets()
    {
        var assets1 = new List<object> { new object(), new object() };
        _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>())).Returns(assets1);
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(assets1.Count);
        _assetPools.Add(_mockPool1.Object);

        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);
        var result = _aggregatingPool.GetAssets(5); // Request 5, but only 2 available
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.SequenceEqual(assets1));
    }

    [Test]
    public void GetAssets_TotalMoreThanRequested_ReturnsRequestedNumberOfAssetsAggregated()
    {
        var assets1 = new List<object> { new object(), new object() }; // Pool 1 has 2 assets
        var assets2 = new List<object> { new object(), new object(), new object() }; // Pool 2 has 3 assets

        _mockPool1.Setup(p => p.GetAssetCount()).Returns(assets1.Count);
        _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets1.Take(count).ToList());

        _mockPool2.Setup(p => p.GetAssetCount()).Returns(assets2.Count);
        _mockPool2.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets2.Take(count).ToList());

        _assetPools.Add(_mockPool1.Object);
        _assetPools.Add(_mockPool2.Object);
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);

        // Request 3 assets. Should get 2 from pool1 and 1 from pool2.
        var result = _aggregatingPool.GetAssets(3);
        Assert.AreEqual(3, result.Count());
        Assert.IsTrue(result.Take(2).SequenceEqual(assets1)); // First 2 from pool1
        Assert.IsTrue(result.Skip(2).SequenceEqual(assets2.Take(1))); // Next 1 from pool2
    }

    [Test]
    public void GetAssets_PoolReturnsFewerAssetsThanCountSuggests_HandlesGracefully()
    {
        var assets1 = new List<object> { new object(), new object() };
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(5); // Reports 5 assets
        _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>())).Returns(assets1); // But only returns 2

        _assetPools.Add(_mockPool1.Object);
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);

        var result = _aggregatingPool.GetAssets(5); // Request 5
        Assert.AreEqual(2, result.Count()); // Should only get 2
        Assert.IsTrue(result.SequenceEqual(assets1));
    }

    [Test]
    public void GetNextAsset_NoAssets_ReturnsDefault()
    {
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools); // No pools added
        Assert.IsNull(_aggregatingPool.GetNextAsset()); // Assuming default(T) is null for object
    }

    [Test]
    public void GetNextAsset_RetrievesAllAssetsSequentiallyFromMultiplePools()
    {
        var asset1 = new object();
        var asset2 = new object();
        var asset3 = new object();

        _mockPool1.SetupSequence(p => p.GetNextAsset())
            .Returns(asset1)
            .Returns(asset2)
            .Returns(default(object)); // Pool 1 exhausted

        _mockPool2.SetupSequence(p => p.GetNextAsset())
            .Returns(asset3)
            .Returns(default(object)); // Pool 2 exhausted

        _assetPools.Add(_mockPool1.Object);
        _assetPools.Add(_mockPool2.Object);
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);

        Assert.AreSame(asset1, _aggregatingPool.GetNextAsset());
        Assert.AreSame(asset2, _aggregatingPool.GetNextAsset());
        Assert.AreSame(asset3, _aggregatingPool.GetNextAsset());
        Assert.IsNull(_aggregatingPool.GetNextAsset()); // All assets retrieved
    }

    [Test]
    public void GetNextAsset_PoolExhausted_SwitchesToNextPool()
    {
        var asset1 = new object();
        var asset2 = new object();

        _mockPool1.SetupSequence(p => p.GetNextAsset())
            .Returns(asset1)
            .Returns(default(object)); // Pool 1 exhausted

        _mockPool2.Setup(p => p.GetNextAsset()).Returns(asset2);

        _assetPools.Add(_mockPool1.Object);
        _assetPools.Add(_mockPool2.Object);
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);

        Assert.AreSame(asset1, _aggregatingPool.GetNextAsset()); // From pool 1
        Assert.AreSame(asset2, _aggregatingPool.GetNextAsset()); // From pool 2
    }

    [Test]
    public void GetNextAsset_CalledWhenNoAssetsAvailableAfterSomeRetrievals_ReturnsDefault()
    {
        var asset1 = new object();
        _mockPool1.SetupSequence(p => p.GetNextAsset())
            .Returns(asset1)
            .Returns(default(object));
        _assetPools.Add(_mockPool1.Object);
        _aggregatingPool = new AggregatingAssetPool<object>(_assetPools);

        Assert.AreSame(asset1, _aggregatingPool.GetNextAsset()); // Retrieve one asset
        Assert.IsNull(_aggregatingPool.GetNextAsset()); // No more assets
        Assert.IsNull(_aggregatingPool.GetNextAsset()); // Called again, should still be null
    }
}
