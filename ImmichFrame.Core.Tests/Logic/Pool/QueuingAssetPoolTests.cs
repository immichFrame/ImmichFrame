using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Logic.Pool;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class QueuingAssetPoolTests
{
    private Mock<IAssetPool> _mockDelegatePool;
    private QueuingAssetPool _queuingAssetPool;

    private const int ReloadBatchSize = 50; // Matches const in QueuingAssetPool
    private const int ReloadThreshold = 10; // Matches const in QueuingAssetPool

    [SetUp]
    public void Setup()
    {
        var logger = FixtureHelpers.TestLogger<QueuingAssetPool>();
        _mockDelegatePool = new Mock<IAssetPool>();

        // QueuingAssetPool inherits from AggregatingAssetPool, which has a constructor
        // expecting IEnumerable<IAssetPool>. However, the QueuingAssetPool constructor
        // only takes a single IAssetPool delegate. We pass the delegate in an array.
        _queuingAssetPool = new QueuingAssetPool(logger, _mockDelegatePool.Object);
    }

    private List<AssetResponseDto> CreateSampleAssets(int count, string prefix = "asset")
    {
        var assets = new List<AssetResponseDto>();
        for (int i = 0; i < count; i++)
        {
            assets.Add(new AssetResponseDto { Id = $"{prefix}_{i}", Type = AssetTypeEnum.IMAGE });
        }

        return assets;
    }

    [Test]
    public async Task GetAssetCount_DelegatesToInnerPool()
    {
        // Arrange
        long expectedCount = 123;
        _mockDelegatePool.Setup(dp => dp.GetAssetCount(It.IsAny<CancellationToken>())).ReturnsAsync(expectedCount);

        // Act
        var actualCount = await _queuingAssetPool.GetAssetCount();

        // Assert
        Assert.That(actualCount, Is.EqualTo(expectedCount));
        _mockDelegatePool.Verify(dp => dp.GetAssetCount(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetNextAsset_RetrievesFromInitiallyEmptyQueue_TriggersReload_ReturnsAsset()
    {
        // Arrange
        var assetToReturn = CreateSampleAssets(1, "initial_load").First();
        _mockDelegatePool.Setup(dp => dp.GetAssets(ReloadBatchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetResponseDto> { assetToReturn })
            .Verifiable();

        // Act
        var result = await _queuingAssetPool.GetNextAssetForTesting(CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(assetToReturn.Id));
        _mockDelegatePool.Verify(dp => dp.GetAssets(ReloadBatchSize, It.IsAny<CancellationToken>()), Times.Once, "ReloadAssetsAsync should have been called as queue was empty.");
    }

    [Test]
    public async Task GetNextAsset_QueueAboveThreshold_DoesNotTriggerReload()
    {
        // Arrange
        // Pre-fill the queue to be above the threshold
        var initialAssets = CreateSampleAssets(ReloadThreshold + 5, "prefill");
        foreach (var asset in initialAssets)
        {
            await _queuingAssetPool.WriteToChannelForTesting(asset);
        }

        _mockDelegatePool.Setup(dp => dp.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetResponseDto>()); // Should not be called

        // Act
        var result = await _queuingAssetPool.GetNextAssetForTesting(CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(initialAssets.First().Id));
        _mockDelegatePool.Verify(dp => dp.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never, "ReloadAssetsAsync should not be called when queue is above threshold.");
    }

    [Test]
    public async Task GetNextAsset_QueueDropsBelowThreshold_TriggersReload()
    {
        // Arrange
        // Pre-fill queue to threshold + 1. One read will take it to threshold, next below.
        var initialAssetsCount = ReloadThreshold + 1;
        var initialAssets = CreateSampleAssets(initialAssetsCount, "threshold_test");
        foreach (var asset in initialAssets)
        {
            await _queuingAssetPool.WriteToChannelForTesting(asset);
        }

        var newAssetsToLoad = CreateSampleAssets(5, "reloaded");
        _mockDelegatePool.Setup(dp => dp.GetAssets(ReloadBatchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newAssetsToLoad)
            .Verifiable(); // This should be called

        // Act & Assert
        // Read one asset, queue count becomes ReloadThreshold. No reload yet.
        await _queuingAssetPool.GetNextAssetForTesting(CancellationToken.None);
        _mockDelegatePool.Verify(dp => dp.GetAssets(ReloadBatchSize, It.IsAny<CancellationToken>()), Times.Never, "Reload should not happen when count is AT threshold.");

        // Read another asset, queue count becomes ReloadThreshold - 1. Reload should be triggered.
        await _queuingAssetPool.GetNextAssetForTesting(CancellationToken.None);
        _mockDelegatePool.Verify(dp => dp.GetAssets(ReloadBatchSize, It.IsAny<CancellationToken>()), Times.Once, "Reload should happen when count is BELOW threshold.");
    }

    [Test]
    public async Task ReloadAssetsAsync_PreventsConcurrentReloads()
    {
        // Arrange
        _mockDelegatePool.Setup(dp => dp.GetAssets(ReloadBatchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                // Simulate delay in fetching assets
                Task.Delay(100).Wait();
                return CreateSampleAssets(ReloadBatchSize, "slow_load");
            });

        // Act
        // Trigger two reloads almost simultaneously.
        // Since GetNextAsset calls ReloadAssetsAsync in a fire-and-forget way,
        // we need to call ReloadAssetsAsync directly for this test or use GetNextAsset and manage timing.
        // We'll call GetNextAsset, which internally calls ReloadAssetsAsync.
        // The semaphore in ReloadAssetsAsync should prevent concurrent execution.

        // Call GetNextAsset twice. The first will trigger reload.
        // The second, if called while the first is "running", should see the semaphore locked.
        var task1 = _queuingAssetPool.GetAssets(1, CancellationToken.None);
        var task2 = _queuingAssetPool.GetAssets(1, CancellationToken.None);

        await Task.WhenAll(task1, task2);

        // Assert
        // The delegate's GetAssets should only be called once due to semaphore.
        _mockDelegatePool.Verify(dp => dp.GetAssets(ReloadBatchSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetNextAsset_HandlesOperationCanceledException_ReturnsNull()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        // Ensure queue is empty so ReadAsync is awaited

        // Act: Call GetNextAsset with a token that will be cancelled
        var getAssetTask = _queuingAssetPool.GetNextAssetForTesting(cts.Token);
        cts.Cancel(); // Cancel the operation
        var result = await getAssetTask;

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ReloadAssetsAsync_AddsFetchedAssetsToQueue()
    {
        // Arrange
        var assetsToLoad = CreateSampleAssets(5, "reloaded_assets");
        _mockDelegatePool.Setup(dp => dp.GetAssets(ReloadBatchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assetsToLoad);

        // Act
        // Trigger reload by reading from empty queue
        await _queuingAssetPool.GetNextAssetForTesting(CancellationToken.None);

        // Wait for the fire-and-forget ReloadAssetsAsync to potentially complete
        // This is tricky. A small delay might work for testing but isn't robust.
        // Better: check queue count or read multiple items.
        await Task.Delay(50); // Give some time for the background reload to process

        // Assert
        // Try to read all loaded assets + the first one that triggered the load.
        var retrievedAssets = new List<AssetResponseDto>();
        retrievedAssets.Add(assetsToLoad.First()); // The one returned by GetNextAsset

        for (int i = 1; i < assetsToLoad.Count; i++) // Read remaining from queue
        {
            // Use a timeout for reading from channel in case reload didn't populate as expected
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            try
            {
                var asset = await _queuingAssetPool.GetNextAssetForTesting(timeoutCts.Token);
                if (asset != null) retrievedAssets.Add(asset);
                else break;
            }
            catch (OperationCanceledException) // Catch timeout
            {
                break;
            }
        }

        Assert.That(retrievedAssets.Count, Is.EqualTo(assetsToLoad.Count), "Should retrieve all assets loaded by ReloadAssetsAsync.");
        foreach (var loadedAsset in assetsToLoad)
        {
            Assert.That(retrievedAssets.Any(ra => ra.Id == loadedAsset.Id), Is.True, $"Asset {loadedAsset.Id} not found in retrieved assets.");
        }
    }
}

// Helper extension or make methods in QueuingAssetPool internal/public for testing
public static class QueuingAssetPoolTestExtensions
{
    // Expose GetNextAsset for testing (it's protected in AggregatingAssetPool)
    public static Task<AssetResponseDto?> GetNextAssetForTesting(this QueuingAssetPool pool, CancellationToken ct)
    {
        // Using reflection to call protected GetNextAsset
        var methodInfo = typeof(AggregatingAssetPool).GetMethod("GetNextAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (Task<AssetResponseDto?>)methodInfo.Invoke(pool, new object[] { ct });
    }

    // Helper to write to channel for test setup
    public static async Task WriteToChannelForTesting(this QueuingAssetPool pool, AssetResponseDto asset)
    {
        var fieldInfo = typeof(QueuingAssetPool).GetField("_assetQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var channel = (Channel<AssetResponseDto>)fieldInfo.GetValue(pool);
        await channel.Writer.WriteAsync(asset);
    }
}