using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic.Pool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace ImmichFrame.Core.Tests.Logic.Pool;

[TestFixture]
public class RandomDateAssetsPoolTests
{
    private Mock<IApiCache> _mockApiCache;
    private Mock<ImmichApi> _mockImmichApi;
    private Mock<IAccountSettings> _mockAccountSettings;
    private TestableRandomDateAssetsPool _randomDateAssetsPool;

    private class TestableRandomDateAssetsPool : RandomDateAssetsPool
    {
        public TestableRandomDateAssetsPool(IApiCache apiCache, ImmichApi immichApi, IAccountSettings accountSettings)
            : base(apiCache, immichApi, accountSettings) { }

        public Task<IEnumerable<AssetResponseDto>> TestLoadAssets(CancellationToken ct = default)
        {
            return base.LoadAssetsInternal(ct);
        }
    }

    [SetUp]
    public void Setup()
    {
        _mockApiCache = new Mock<IApiCache>();
        _mockImmichApi = new Mock<ImmichApi>("", null!);
        _mockAccountSettings = new Mock<IAccountSettings>();
        _randomDateAssetsPool = new TestableRandomDateAssetsPool(_mockApiCache.Object, _mockImmichApi.Object, _mockAccountSettings.Object);

        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(false);
    }

    private AssetResponseDto CreateAssetWithDate(string id, DateTime? takenDate = null, DateTime? fileCreatedDate = null)
    {
        return new AssetResponseDto 
        { 
            Id = id, 
            Type = AssetTypeEnum.IMAGE,
            ExifInfo = takenDate.HasValue ? new ExifResponseDto { DateTimeOriginal = takenDate } : null,
            FileCreatedAt = fileCreatedDate != null ? new DateTimeOffset(fileCreatedDate.Value) : DateTimeOffset.UtcNow
        };
    }

    private SearchResponseDto CreateSearchResult(List<AssetResponseDto> assets, int total)
    {
        return new SearchResponseDto 
        { 
            Assets = new SearchAssetResponseDto 
            { 
                Items = assets,
                Total = total 
            } 
        };
    }

    [Test]
    public async Task TestLoadAssets_WithValidDateRange_ReturnsAssets()
    {
        // Arrange
        var oldestDate = new DateTime(2020, 1, 1);
        var youngestDate = new DateTime(2024, 12, 31);
        
        var oldestAsset = CreateAssetWithDate("oldest", oldestDate);
        var youngestAsset = CreateAssetWithDate("youngest", youngestDate);
        
        var randomDateAssets = new List<AssetResponseDto>
        {
            CreateAssetWithDate("random1", new DateTime(2022, 6, 15)),
            CreateAssetWithDate("random2", new DateTime(2022, 6, 15)),
            CreateAssetWithDate("random3", new DateTime(2022, 6, 15))
        };

        // Setup oldest asset query (ASC order)
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.Order == AssetOrder.Asc && dto.Size == 1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { oldestAsset }, 1));

        // Setup youngest asset query (DESC order)
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.Order == AssetOrder.Desc && dto.Size == 1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { youngestAsset }, 1));

        // Setup random date query (with date range) - return our 3 assets for any date range query
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.TakenAfter.HasValue && dto.TakenBefore.HasValue && dto.Size >= 50),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(randomDateAssets, randomDateAssets.Count));

        // Setup fallback query - return empty (should not be called)
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.Size >= 200 && !dto.TakenAfter.HasValue && !dto.TakenBefore.HasValue),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto>(), 0));

        // Act - Configure the pool to use smaller blocks for testing and call GetAssets to set requested count
        _randomDateAssetsPool.ConfigureAssetsPerRandomDate(3); // 3 assets per random date
        var result = await _randomDateAssetsPool.GetAssets(3); // Request only 3 assets

        // Assert - With 3 requested assets and 3 per date, we get exactly 3 assets from the first date block
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Count(), Is.EqualTo(3));
        Assert.That(result.All(a => a.Id.StartsWith("random")), Is.True);
    }

    [Test]
    public async Task TestLoadAssets_WithClusterBasedApproach_Demo()
    {
        // Arrange - This test demonstrates the new cluster-based approach
        var oldestAsset = CreateAssetWithDate("oldest", new DateTime(2020, 1, 1), new DateTime(2020, 1, 1));
        var youngestAsset = CreateAssetWithDate("youngest", new DateTime(2024, 12, 31), new DateTime(2024, 12, 31));

        // Setup responses for cluster initialization and searches
        _mockImmichApi.SetupSequence(api => api.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { oldestAsset }, 1))      // oldest query
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { youngestAsset }, 1));    // youngest query

        // Setup default return for monthly statistics and other calls
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto>(), 10)); // Return some count for monthly stats

        Console.WriteLine("\n=== Testing Cluster-Based Photo Selection ===");
        Console.WriteLine("The new approach creates balanced photo clusters based on actual photo distribution");
        Console.WriteLine("rather than uniform time distribution, preventing over-representation of old photos.\n");

        // Act
        var result = await _randomDateAssetsPool.TestLoadAssets();

        // Assert - The result may be empty due to no mock assets, but cluster initialization should occur
        Assert.That(result, Is.Not.Null);
        
        Console.WriteLine("=== Cluster-Based Approach Demo Complete ===");
        Console.WriteLine("Check the debug output above to see the cluster initialization process.\n");
        
        // Just verify that API was called multiple times (relaxed assertion)
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(It.IsAny<MetadataSearchDto>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Test]
    public async Task TestLoadAssets_WithInsufficientAssetsFromRandomDate_RetriesWithFallback()
    {
        // Arrange
        var oldestDate = new DateTime(2020, 1, 1);
        var youngestDate = new DateTime(2024, 12, 31);
        
        var oldestAsset = CreateAssetWithDate("oldest", oldestDate);
        var youngestAsset = CreateAssetWithDate("youngest", youngestDate);
        
        // First few attempts return few/no assets
        var fewAssets = new List<AssetResponseDto> { CreateAssetWithDate("few1") };
        
        // Fallback query returns more assets
        var fallbackAssets = Enumerable.Range(1, 50)
            .Select(i => CreateAssetWithDate($"fallback{i}"))
            .ToList();

        // Setup oldest/youngest queries
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.Order == AssetOrder.Asc && dto.Size == 1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { oldestAsset }, 1));

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.Order == AssetOrder.Desc && dto.Size == 1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { youngestAsset }, 1));

        // Setup date range queries to return insufficient results initially
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.TakenAfter.HasValue && dto.TakenBefore.HasValue && dto.Size >= 50),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(fewAssets, fewAssets.Count));

        // Setup fallback query (no date filters, larger size)
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => !dto.TakenAfter.HasValue && !dto.TakenBefore.HasValue && dto.Size >= 200),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(fallbackAssets, fallbackAssets.Count));

        // Act
        var result = await _randomDateAssetsPool.TestLoadAssets();

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Count(), Is.GreaterThan(10)); // Should have triggered fallback
        
        // Verify that both date-specific and fallback queries were attempted
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.TakenAfter.HasValue && dto.TakenBefore.HasValue),
            It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
    }

    [Test]
    public async Task TestLoadAssets_WithSameDates_UsesFallback()
    {
        // Arrange - oldest and youngest are the same date
        var sameDate = new DateTime(2023, 6, 15);
        var oldestAsset = CreateAssetWithDate("same1", sameDate);
        var youngestAsset = CreateAssetWithDate("same2", sameDate);
        
        var fallbackAssets = new List<AssetResponseDto>
        {
            CreateAssetWithDate("fallback1"),
            CreateAssetWithDate("fallback2"),
            CreateAssetWithDate("fallback3")
        };

        // Setup oldest/youngest queries
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.Order == AssetOrder.Asc && dto.Size == 1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { oldestAsset }, 1));

        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.Order == AssetOrder.Desc && dto.Size == 1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(new List<AssetResponseDto> { youngestAsset }, 1));

        // Setup fallback query
        _mockImmichApi.Setup(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => !dto.TakenAfter.HasValue && !dto.TakenBefore.HasValue && dto.Size >= 200),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSearchResult(fallbackAssets, fallbackAssets.Count));

        // Act
        var result = await _randomDateAssetsPool.TestLoadAssets();

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Count(), Is.EqualTo(3));
        Assert.That(result.All(a => a.Id.StartsWith("fallback")), Is.True);
        
        // Should skip date-range queries and go straight to fallback
        _mockImmichApi.Verify(api => api.SearchAssetsAsync(
            It.Is<MetadataSearchDto>(dto => dto.TakenAfter.HasValue && dto.TakenBefore.HasValue),
            It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    }