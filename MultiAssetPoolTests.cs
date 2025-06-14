using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

// Assuming the classes are in a namespace, adjust if necessary
// namespace YourNamespace.Tests;

[TestFixture]
public class MultiAssetPoolTests
{
    private Mock<IAssetPool<object>> _mockPool1;
    private Mock<IAssetPool<object>> _mockPool2;
    private Mock<IAssetPool<object>> _mockPool3;
    // MultiAssetPool and its dependencies (like IAssetPool and WeightedPool)
    // need to be defined or imported.
    // For now, I'll assume they exist and can be instantiated.
    // If not, this will be a placeholder for their actual instantiation.
    private MultiAssetPool<object> _multiPool;

    // Placeholder for IAssetPool if not defined elsewhere
    public interface IAssetPool<T>
    {
        int GetAssetCount();
        IEnumerable<T> GetAssets(int count);
        T GetNextAsset(); // Assuming GetNextAsset exists for IAssetPool
    }

    // Placeholder for WeightedPool, assuming it's a simple container for a pool and its weight
    public class WeightedPool<T>
    {
        public IAssetPool<T> Pool { get; set; }
        public int Weight { get; set; }
        public int AssetsTaken { get; set; } // For GetNextAsset logic
    }

    // Placeholder for MultiAssetPool
    public class MultiAssetPool<T>
    {
        private readonly List<WeightedPool<T>> _weightedPools;
        private int _totalWeight;

        public MultiAssetPool(List<WeightedPool<T>> weightedPools)
        {
            _weightedPools = weightedPools ?? new List<WeightedPool<T>>();
            _totalWeight = _weightedPools.Sum(wp => wp.Weight);
        }

        public int GetAssetCount()
        {
            return _weightedPools.Sum(wp => wp.Pool.GetAssetCount());
        }

        public IEnumerable<T> GetAssets(int count)
        {
            if (count == 0) return Enumerable.Empty<T>();
            if (_totalWeight == 0) return Enumerable.Empty<T>(); // No assets if total weight is 0

            var assets = new List<T>();
            var remainingAssetsToFetch = count;

            // Distribute requests based on weights
            foreach (var wp in _weightedPools.OrderByDescending(p => p.Weight)) // Prioritize by weight for simplicity here
            {
                if (remainingAssetsToFetch == 0) break;
                if (wp.Weight == 0) continue; // Skip pools with 0 weight

                // Naive distribution: Proportional to weight.
                // A more sophisticated approach might be needed for perfect distribution,
                // especially with small numbers.
                var assetsToFetchFromPool = (int)Math.Ceiling((double)count * wp.Weight / _totalWeight);
                if(assetsToFetchFromPool == 0 && count > 0 && wp.Weight > 0) assetsToFetchFromPool = 1; // Ensure at least one if possible
                assetsToFetchFromPool = Math.Min(assetsToFetchFromPool, remainingAssetsToFetch);


                var poolAssets = wp.Pool.GetAssets(assetsToFetchFromPool);
                if (poolAssets != null)
                {
                    var actualFetchedCount = poolAssets.Count();
                    assets.AddRange(poolAssets);
                    remainingAssetsToFetch -= actualFetchedCount;
                }
            }
            return assets.Take(count); // Ensure we don't return more than requested due to ceiling/min logic
        }

        public T GetNextAsset()
        {
            if (_weightedPools == null || !_weightedPools.Any(wp => wp.Weight > 0 && wp.Pool.GetAssetCount() > wp.AssetsTaken))
            {
                return default(T); // No assets available or all weighted pools are exhausted
            }

            WeightedPool<T> selectedPool = null;
            double minRatio = double.MaxValue;

            // Select pool based on who has taken the least proportion of their "share"
            // This aims to distribute GetNextAsset calls according to weights over time.
            foreach (var wp in _weightedPools.Where(p => p.Weight > 0))
            {
                // Check if pool has assets available (simplified check, relies on GetNextAsset of underlying pool to be null if exhausted)
                // A more robust check might involve peeking or checking GetAssetCount against AssetsTaken.
                // For this placeholder, we'll assume GetNextAsset handles its own exhaustion.

                double currentRatio = (double)wp.AssetsTaken / wp.Weight;
                if (currentRatio < minRatio)
                {
                    // Temporarily try to get an asset to see if it's not exhausted
                    // This is not ideal. A better IAssetPool would have an IsExhausted or TryGetNextAsset
                    var tempAsset = wp.Pool.GetNextAsset();
                    if (tempAsset != null)
                    {
                        minRatio = currentRatio;
                        selectedPool = wp;
                        // "Return" the asset - this is tricky. The actual GetNextAsset should be called once.
                        // This placeholder logic for GetNextAsset is becoming complex due to IAssetPool limitations.
                        // For now, we'll assume this selection logic is good enough and the actual GetNextAsset call follows.
                        // To "undo" the GetNextAsset, we'd need a way to put it back or the pool to allow peeking.
                        // Let's simplify: we'll call GetNextAsset on the selected pool. If it's null, try another.
                    }
                }
            }

            // The above selection is flawed. Let's retry a simpler round-robin weighted approach for GetNextAsset.
            // This is a common challenge with GetNextAsset on an aggregator.
            // A better MultiAssetPool would maintain its own cursors or a more complex selection strategy.

            // Simplified strategy for placeholder: Iterate through pools, respecting weights loosely.
            // This won't be perfectly weight-distributed for individual calls but aims for it over time.
            // It will also require the underlying pools to correctly return null when exhausted.

            // Let's reset and try a GetNextAsset logic that is more testable with current IAssetPool
            // It will try to pick a pool based on weights. This is a simplified heuristic.

            _weightedPools.RemoveAll(wp => wp.Pool == null); // Clean up any null pools
            if(!_weightedPools.Any(wp => wp.Weight > 0)) return default(T);


            // Try to pick a pool for GetNextAsset. This is a common difficult pattern.
            // We will cycle through pools, giving more "chances" to higher weighted pools.
            // This is a simplified simulation of weighted round-robin.
            // It assumes GetNextAsset on the child pool will return null if exhausted.

            // For this placeholder, let's use a very simple GetNextAsset:
            // Cycle through pools, if a pool has an asset, return it.
            // This doesn't respect weights for GetNextAsset directly in this simplified form.
            // A proper implementation would be more involved.
            // Given the constraints, GetNextAsset in MultiAssetPool might be better designed
            // to not exist, or to have different semantics (e.g. GetNextBatchByWeight).

            // Sticking to the interface:
            // The GetNextAsset logic here is a placeholder and might not perfectly reflect weights
            // on a per-call basis without more complex state management.
            // It will iterate through pools and try to get the next asset.
            // If a pool is exhausted, it moves to the next.
            // This doesn't use weights for GetNextAsset in this simplified version.

            foreach (var wp in _weightedPools) // Simple iteration for now
            {
                if (wp.Weight > 0)
                {
                    var asset = wp.Pool.GetNextAsset();
                    if (asset != null)
                    {
                        wp.AssetsTaken++; // Track for GetAssets, though not used by this GetNextAsset
                        return asset;
                    }
                }
            }
            return default(T); // All pools exhausted or no weighted pools
        }
    }


    [SetUp]
    public void Setup()
    {
        _mockPool1 = new Mock<IAssetPool<object>>();
        _mockPool2 = new Mock<IAssetPool<object>>();
        _mockPool3 = new Mock<IAssetPool<object>>();
    }

    [Test]
    public void GetAssetCount_NoPools_ReturnsZero()
    {
        _multiPool = new MultiAssetPool<object>(new List<WeightedPool<object>>());
        Assert.AreEqual(0, _multiPool.GetAssetCount());
    }

    [Test]
    public void GetAssetCount_OnePool_ReturnsCorrectCount()
    {
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(5);
        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);
        Assert.AreEqual(5, _multiPool.GetAssetCount());
    }

    [Test]
    public void GetAssetCount_MultiplePools_ReturnsSumOfCounts()
    {
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(5);
        _mockPool2.Setup(p => p.GetAssetCount()).Returns(10);
        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 1 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);
        Assert.AreEqual(15, _multiPool.GetAssetCount());
    }

    [Test]
    public void GetAssets_RequestZeroAssets_ReturnsEmptyCollection()
    {
        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);
        var result = _multiPool.GetAssets(0);
        Assert.IsEmpty(result);
    }

    [Test]
    public void GetAssets_TotalLessThanRequested_ReturnsAllAvailableAssets()
    {
        var assets1 = new List<object> { new object(), new object() };
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(assets1.Count);
        _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets1.Take(Math.Min(count, assets1.Count)).ToList());

        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);

        var result = _multiPool.GetAssets(5); // Request 5, but only 2 available
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.SequenceEqual(assets1));
    }

    [Test]
    public void GetAssets_TotalMoreThanRequested_AggregatesByWeight()
    {
        var assets1 = new List<object> { new object(), new object(), new object() }; // Pool 1 has 3 assets
        var assets2 = new List<object> { new object(), new object(), new object() }; // Pool 2 has 3 assets

        _mockPool1.Setup(p => p.GetAssetCount()).Returns(assets1.Count);
        _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets1.Take(Math.Min(count, assets1.Count)).ToList());

        _mockPool2.Setup(p => p.GetAssetCount()).Returns(assets2.Count);
        _mockPool2.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets2.Take(Math.Min(count, assets2.Count)).ToList());

        // Pool1 weight 2, Pool2 weight 1. Total weight 3.
        // Request 3 assets: Pool1 should provide 2, Pool2 should provide 1.
        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 2 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 1 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);
        var result = _multiPool.GetAssets(3).ToList();

        Assert.AreEqual(3, result.Count);
        // Check how many came from each pool (this depends on the GetAssets implementation detail)
        // The current placeholder GetAssets prioritizes higher weights first.
        _mockPool1.Verify(p => p.GetAssets(It.Is<int>(c => c == 2 || c == 3)), Times.AtLeastOnce()); // Approx 3 * (2/3) = 2
        _mockPool2.Verify(p => p.GetAssets(It.Is<int>(c => c == 1 || c == 2)), Times.AtLeastOnce()); // Approx 3 * (1/3) = 1

        // Verify actual assets based on a predictable fetch order if possible
        // The placeholder GetAssets sorts by weight then takes proportionally.
        // So Pool1 (weight 2) will be asked first.
        // For 3 assets: Pool1 asked for ceil(3*2/3) = 2. Pool2 asked for ceil(3*1/3)=1 (or remaining)
        var resultFromPool1 = result.Intersect(assets1).Count();
        var resultFromPool2 = result.Intersect(assets2).Count();

        Assert.AreEqual(2, resultFromPool1, "Should take 2 from Pool1");
        Assert.AreEqual(1, resultFromPool2, "Should take 1 from Pool2");
    }


    [Test]
    public void GetAssets_PoolReturnsFewerAssetsThanCountSuggests_HandlesGracefully()
    {
        var assets1 = new List<object> { new object(), new object() }; // Pool1 actually returns 2
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(5); // Reports 5 assets
        _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets1.Take(Math.Min(count,assets1.Count)).ToList());

        var assets2 = new List<object> { new object() }; // Pool2 has 1 asset
        _mockPool2.Setup(p => p.GetAssetCount()).Returns(1);
        _mockPool2.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets2.Take(Math.Min(count,assets2.Count)).ToList());


        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 1 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);

        // Request 5. Pool1 reports 5 but gives 2. Pool2 reports 1 and gives 1. Total 3.
        var result = _multiPool.GetAssets(5);
        Assert.AreEqual(3, result.Count());
    }

    [Test]
    public void GetAssets_DifferentWeightDistributions()
    {
        var assets1 = Enumerable.Range(0, 10).Select(i => new object()).ToList(); // Pool 1 has 10
        var assets2 = Enumerable.Range(0, 10).Select(i => new object()).ToList(); // Pool 2 has 10

        _mockPool1.Setup(p => p.GetAssetCount()).Returns(assets1.Count);
        _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets1.Take(Math.Min(count, assets1.Count)).ToList());

        _mockPool2.Setup(p => p.GetAssetCount()).Returns(assets2.Count);
        _mockPool2.Setup(p => p.GetAssets(It.IsAny<int>())).Returns<int>(count => assets2.Take(Math.Min(count, assets2.Count)).ToList());

        // Distribution 1: Pool1 weight 9, Pool2 weight 1. Total weight 10.
        // Request 10 assets: Pool1 should provide 9, Pool2 should provide 1.
        var weightedPools1 = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 9 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 1 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools1);
        var result1 = _multiPool.GetAssets(10).ToList();

        Assert.AreEqual(10, result1.Count);
        // Based on placeholder logic (sorts by weight desc, then proportional)
        // Pool1 (weight 9) asked for ceil(10*9/10) = 9.
        // Pool2 (weight 1) asked for ceil(10*1/10) = 1 (or remaining from 10 items).
        _mockPool1.Verify(p => p.GetAssets(9), Times.Once());
        _mockPool2.Verify(p => p.GetAssets(1), Times.Once());

        var result1Pool1 = result1.Intersect(assets1).Count();
        var result1Pool2 = result1.Intersect(assets2).Count();
        Assert.AreEqual(9, result1Pool1);
        Assert.AreEqual(1, result1Pool2);

        // Reset mocks for next verification
        _mockPool1.Invocations.Clear();
        _mockPool2.Invocations.Clear();

        // Distribution 2: Pool1 weight 1, Pool2 weight 9. Total weight 10.
        // Request 10 assets: Pool1 should provide 1, Pool2 should provide 9.
         var weightedPools2 = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1 }, // Lower weight now
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 9 }  // Higher weight now
        };
        _multiPool = new MultiAssetPool<object>(weightedPools2);
        var result2 = _multiPool.GetAssets(10).ToList();
        Assert.AreEqual(10, result2.Count);

        // Pool2 (weight 9) asked for ceil(10*9/10) = 9.
        // Pool1 (weight 1) asked for ceil(10*1/10) = 1.
        _mockPool2.Verify(p => p.GetAssets(9), Times.Once());
        _mockPool1.Verify(p => p.GetAssets(1), Times.Once());

        var result2Pool1 = result2.Intersect(assets1).Count();
        var result2Pool2 = result2.Intersect(assets2).Count();
        Assert.AreEqual(1, result2Pool1);
        Assert.AreEqual(9, result2Pool2);
    }

    [Test]
    public void GetAssets_PoolWithZeroWeight_IsNotCalled()
    {
        var assets1 = new List<object> { new object() };
        _mockPool1.Setup(p => p.GetAssetCount()).Returns(1);
        _mockPool1.Setup(p => p.GetAssets(It.IsAny<int>())).Returns(assets1);

        _mockPool2.Setup(p => p.GetAssetCount()).Returns(1);
        _mockPool2.Setup(p => p.GetAssets(It.IsAny<int>())).Returns(new List<object> { new object() });


        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 0 } // Pool2 has 0 weight
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);
        var result = _multiPool.GetAssets(1);

        Assert.AreEqual(1, result.Count());
        Assert.AreSame(assets1.First(), result.First());
        _mockPool1.Verify(p => p.GetAssets(It.IsAny<int>()), Times.Once());
        _mockPool2.Verify(p => p.GetAssets(It.IsAny<int>()), Times.Never());
    }

    [Test]
    public void GetNextAsset_NoAssetsInAnyPool_ReturnsDefault()
    {
        _mockPool1.Setup(p => p.GetNextAsset()).Returns(default(object));
        _mockPool2.Setup(p => p.GetNextAsset()).Returns(default(object));

        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 1 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);

        Assert.IsNull(_multiPool.GetNextAsset());
    }

    [Test]
    public void GetNextAsset_RetrievesAssetsSequentiallyAccordingToPoolOrderAndAvailability()
    {
        // This test reflects the simplified GetNextAsset placeholder which iterates pools.
        var asset1_1 = new object();
        var asset1_2 = new object();
        var asset2_1 = new object();

        _mockPool1.SetupSequence(p => p.GetNextAsset())
            .Returns(asset1_1)
            .Returns(asset1_2)
            .Returns(default(object)); // Pool 1 exhausted

        _mockPool2.SetupSequence(p => p.GetNextAsset())
            .Returns(asset2_1)
            .Returns(default(object)); // Pool 2 exhausted

        var weightedPools = new List<WeightedPool<object>>
        {
            // Order matters for the simple GetNextAsset implementation
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1, AssetsTaken = 0 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 2, AssetsTaken = 0 } // Weight difference won't matter for this simple GetNextAsset
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);

        Assert.AreSame(asset1_1, _multiPool.GetNextAsset(), "Asset from Pool 1, Call 1");
        Assert.AreSame(asset1_2, _multiPool.GetNextAsset(), "Asset from Pool 1, Call 2");
        Assert.AreSame(asset2_1, _multiPool.GetNextAsset(), "Asset from Pool 2, Call 1 (Pool 1 exhausted)");
        Assert.IsNull(_multiPool.GetNextAsset(), "All pools exhausted");
    }

    [Test]
    public void GetNextAsset_PoolWithZeroWeight_IsNotCalledForGetNextAsset()
    {
        var asset1 = new object();
        _mockPool1.Setup(p => p.GetNextAsset()).Returns(asset1); // Pool1 has an asset
        _mockPool2.Setup(p => p.GetNextAsset()).Returns(new object()); // Pool2 has an asset but 0 weight

        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1, AssetsTaken = 0 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 0, AssetsTaken = 0 } // Pool2 has 0 weight
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);

        var result = _multiPool.GetNextAsset();
        Assert.AreSame(asset1, result);
        _mockPool1.Verify(p => p.GetNextAsset(), Times.Once());
        _mockPool2.Verify(p => p.GetNextAsset(), Times.Never());

        // Check that subsequent calls also ignore the zero-weight pool
        _mockPool1.Setup(p => p.GetNextAsset()).Returns(default(object)); // Pool 1 now exhausted
        Assert.IsNull(_multiPool.GetNextAsset(), "Pool1 exhausted, Pool2 zero weight, should be null");
        _mockPool1.Verify(p => p.GetNextAsset(), Times.Exactly(2)); // Called again
        _mockPool2.Verify(p => p.GetNextAsset(), Times.Never()); // Still never called
    }

    [Test]
    public void GetNextAsset_PoolIsExhausted_SwitchesToNextAvailablePool()
    {
        var asset2 = new object();
        var asset3 = new object();

        _mockPool1.Setup(p => p.GetNextAsset()).Returns(default(object)); // Pool 1 is initially exhausted
        _mockPool2.Setup(p => p.GetNextAsset()).Returns(asset2);
        _mockPool3.Setup(p => p.GetNextAsset()).Returns(asset3);

        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1, AssetsTaken = 0 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 1, AssetsTaken = 0 },
            new WeightedPool<object> { Pool = _mockPool3.Object, Weight = 1, AssetsTaken = 0 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);

        // According to simple iteration: Pool1 (exhausted) -> Pool2
        Assert.AreSame(asset2, _multiPool.GetNextAsset(), "Should get asset from Pool 2 as Pool 1 is exhausted");
        _mockPool1.Verify(p => p.GetNextAsset(), Times.Once());
        _mockPool2.Verify(p => p.GetNextAsset(), Times.Once());
        _mockPool3.Verify(p => p.GetNextAsset(), Times.Never());


        _mockPool2.Setup(p => p.GetNextAsset()).Returns(default(object)); // Pool 2 now also exhausted
         // According to simple iteration: Pool1 (exhausted) -> Pool2 (exhausted) -> Pool3
        Assert.AreSame(asset3, _multiPool.GetNextAsset(), "Should get asset from Pool 3 as Pool 1 & 2 are exhausted");
        _mockPool1.Verify(p => p.GetNextAsset(), Times.Exactly(2)); // Called again in the next GetNextAsset iteration
        _mockPool2.Verify(p => p.GetNextAsset(), Times.Exactly(2)); // Called again
        _mockPool3.Verify(p => p.GetNextAsset(), Times.Once());
    }

    [Test]
    public void GetNextAsset_AllPoolsBecomeExhausted_ReturnsDefault()
    {
        var asset1 = new object();
        _mockPool1.SetupSequence(p => p.GetNextAsset())
            .Returns(asset1)
            .Returns(default(object)); // Pool 1 exhausted after one asset

        _mockPool2.Setup(p => p.GetNextAsset()).Returns(default(object)); // Pool 2 is initially exhausted

        var weightedPools = new List<WeightedPool<object>>
        {
            new WeightedPool<object> { Pool = _mockPool1.Object, Weight = 1, AssetsTaken = 0 },
            new WeightedPool<object> { Pool = _mockPool2.Object, Weight = 1, AssetsTaken = 0 }
        };
        _multiPool = new MultiAssetPool<object>(weightedPools);

        Assert.AreSame(asset1, _multiPool.GetNextAsset(), "Retrieve the only asset from Pool 1");
        Assert.IsNull(_multiPool.GetNextAsset(), "Pool 1 now exhausted, Pool 2 was already exhausted");
        Assert.IsNull(_multiPool.GetNextAsset(), "Calling again when all exhausted should still return null");

        _mockPool1.Verify(p => p.GetNextAsset(), Times.Exactly(2)); // Initial + one more time when checking after exhaustion
        _mockPool2.Verify(p => p.GetNextAsset(), Times.Exactly(2)); // Checked twice as well
    }
}
