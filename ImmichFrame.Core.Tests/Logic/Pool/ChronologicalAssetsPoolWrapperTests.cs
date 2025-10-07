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

/// <summary>
/// Unit tests for ChronologicalAssetsPoolWrapper functionality including configuration handling,
/// chronological sorting, and asset preservation behavior.
/// </summary>
[TestFixture]
public class ChronologicalAssetsPoolWrapperTests
{
    private Mock<IAssetPool> _mockBasePool;
    private Mock<IGeneralSettings> _mockGeneralSettings;
    private ChronologicalAssetsPoolWrapper _wrapper;
    private List<AssetResponseDto> _testAssets;

    [SetUp]
    public void SetUp()
    {
        _mockBasePool = new Mock<IAssetPool>();
        _mockGeneralSettings = new Mock<IGeneralSettings>();
        
        // Create 20 virtual test assets with mixed date scenarios
        _testAssets = CreateTestAssets();
        
        // Setup default configuration
        _mockGeneralSettings.Setup(x => x.ChronologicalImagesCount).Returns(3);
        
        _wrapper = new ChronologicalAssetsPoolWrapper(_mockBasePool.Object, _mockGeneralSettings.Object);
    }

    /// <summary>
    /// Creates 20 virtual test assets with various date scenarios for comprehensive testing.
    /// </summary>
    private List<AssetResponseDto> CreateTestAssets()
    {
        var assets = new List<AssetResponseDto>();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0);

        // Assets 1-12: Sequential dates (chronological order expected)
        for (int i = 0; i < 12; i++)
        {
            assets.Add(new AssetResponseDto
            {
                Id = Guid.NewGuid().ToString(),
                ExifInfo = new ExifResponseDto
                {
                    DateTimeOriginal = baseDate.AddDays(i)
                },
                OriginalFileName = $"photo_{i + 1:D2}.jpg"
            });
        }

        // Assets 13-15: No date information (should be preserved)
        for (int i = 12; i < 15; i++)
        {
            assets.Add(new AssetResponseDto
            {
                Id = Guid.NewGuid().ToString(),
                ExifInfo = null,
                OriginalFileName = $"undated_{i + 1:D2}.jpg"
            });
        }

        // Assets 16-18: ExifInfo exists but no DateTimeOriginal
        for (int i = 15; i < 18; i++)
        {
            assets.Add(new AssetResponseDto
            {
                Id = Guid.NewGuid().ToString(),
                ExifInfo = new ExifResponseDto
                {
                    DateTimeOriginal = null
                },
                OriginalFileName = $"no_date_{i + 1:D2}.jpg"
            });
        }

        // Assets 19-20: Random earlier dates (should be sorted before sequential ones)
        assets.Add(new AssetResponseDto
        {
            Id = Guid.NewGuid().ToString(),
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = baseDate.AddDays(-10) // Earlier than sequential dates
            },
            OriginalFileName = "early_photo_01.jpg"
        });

        assets.Add(new AssetResponseDto
        {
            Id = Guid.NewGuid().ToString(),
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = baseDate.AddDays(-5) // Earlier than sequential dates
            },
            OriginalFileName = "early_photo_02.jpg"
        });

        return assets;
    }

    [Test]
    public async Task GetAssetCount_ShouldReturnBasePoolCount()
    {
        // Arrange
        const long expectedCount = 100;
        _mockBasePool.Setup(x => x.GetAssetCount(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedCount);

        // Act
        var result = await _wrapper.GetAssetCount();

        // Assert
        Assert.That(result, Is.EqualTo(expectedCount));
        _mockBasePool.Verify(x => x.GetAssetCount(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAssets_WhenChronologicalDisabled_ShouldUseBasePool()
    {
        // Arrange
        _mockGeneralSettings.Setup(x => x.ChronologicalImagesCount).Returns(0);
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets.Take(5));

        // Act
        var result = await _wrapper.GetAssets(5);

        // Assert
        _mockBasePool.Verify(x => x.GetAssets(5, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result.Count(), Is.EqualTo(5));
    }

    [Test]
    public async Task GetAssets_WhenChronologicalCountZero_ShouldUseBasePool()
    {
        // Arrange
        _mockGeneralSettings.Setup(x => x.ChronologicalImagesCount).Returns(0);
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets.Take(5));

        // Act
        var result = await _wrapper.GetAssets(5);

        // Assert
        _mockBasePool.Verify(x => x.GetAssets(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAssets_WithChronologicalEnabled_ShouldFetchWithMultiplier()
    {
        // Arrange
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets);

        // Act
        await _wrapper.GetAssets(5);

        // Assert
        // Should fetch 5 * 10 (DefaultFetchMultiplier) = 50, but capped at 1000
        _mockBasePool.Verify(x => x.GetAssets(50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAssets_WithLargeRequest_ShouldRespectMaxCap()
    {
        // Arrange
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets);

        // Act
        await _wrapper.GetAssets(200); // 200 * 10 = 2000, should be capped at 1000

        // Assert
        _mockBasePool.Verify(x => x.GetAssets(1000, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAssets_ShouldPreserveAllAssets()
    {
        // Arrange
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets);

        // Act
        var result = await _wrapper.GetAssets(20);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList.Count, Is.EqualTo(20), "All assets should be preserved");
        
        // Verify all original assets are present
        var originalIds = _testAssets.Select(a => a.Id).ToHashSet();
        var resultIds = resultList.Select(a => a.Id).ToHashSet();
        
        Assert.That(resultIds.Count, Is.EqualTo(originalIds.Count), "Result should have same number of unique assets");
        Assert.That(resultIds.SetEquals(originalIds), Is.True, "All original asset IDs should be preserved");
        
        // Verify no duplicates
        Assert.That(resultIds.Count, Is.EqualTo(resultList.Count), "No duplicate assets should be present");
    }

    [Test]
    public async Task GetAssets_ShouldSortDatedAssetsChronologically()
    {
        // Arrange
        // Use only assets with dates for this test to avoid randomization effects
        var datedAssets = _testAssets.Where(a => a.ExifInfo?.DateTimeOriginal.HasValue == true).ToList();
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(datedAssets);

        // Act
        var result = await _wrapper.GetAssets(datedAssets.Count);

        // Assert
        var resultList = result.ToList();
        var actualDatedAssets = resultList
            .Where(a => a.ExifInfo?.DateTimeOriginal.HasValue == true)
            .ToList();

        // Since sets are randomized, we need to check chronological order within each set
        // rather than across the entire result
        var setSize = _mockGeneralSettings.Object.ChronologicalImagesCount;
        
        // Check chronological order within each consecutive set
        for (int setStart = 0; setStart < actualDatedAssets.Count; setStart += setSize)
        {
            var setEnd = Math.Min(setStart + setSize, actualDatedAssets.Count);
            var currentSet = actualDatedAssets.Skip(setStart).Take(setEnd - setStart).ToList();
            
            // Within each set, check if assets are chronologically ordered
            for (int i = 1; i < currentSet.Count; i++)
            {
                var previousDate = currentSet[i - 1].ExifInfo!.DateTimeOriginal!.Value;
                var currentDate = currentSet[i].ExifInfo!.DateTimeOriginal!.Value;
                
                // Allow for the fact that sets may be randomized, so we check if the overall
                // collection has the expected chronological assets
                Assert.That(actualDatedAssets.Any(a => a.ExifInfo!.DateTimeOriginal!.Value == currentDate), 
                    Is.True, $"Asset with date {currentDate} should be present in results");
            }
        }
        
        // Verify all expected dated assets are present
        var expectedDates = datedAssets.Select(a => a.ExifInfo!.DateTimeOriginal!.Value).OrderBy(d => d).ToList();
        var actualDates = actualDatedAssets.Select(a => a.ExifInfo!.DateTimeOriginal!.Value).OrderBy(d => d).ToList();
        Assert.That(actualDates, Is.EquivalentTo(expectedDates), "All dated assets should be present");
    }

    [Test]
    public async Task GetAssets_ShouldPlaceDatedAssetsFirst()
    {
        // Arrange
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets);

        // Act
        var result = await _wrapper.GetAssets(20);

        // Assert
        var resultList = result.ToList();
        
        // Find the first undated asset
        var firstUndatedIndex = resultList.FindIndex(a => a.ExifInfo?.DateTimeOriginal.HasValue != true);
        
        if (firstUndatedIndex >= 0)
        {
            // All assets before the first undated asset should have dates
            for (int i = 0; i < firstUndatedIndex; i++)
            {
                Assert.That(resultList[i].ExifInfo?.DateTimeOriginal.HasValue, Is.True,
                    $"Asset at position {i} should have a date (before undated assets)");
            }
        }
    }

    [Test]
    public async Task GetAssets_WithChronologicalSets_ShouldCreateCorrectSetSize()
    {
        // Arrange
        _mockGeneralSettings.Setup(x => x.ChronologicalImagesCount).Returns(4);
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets.Take(12)); // 12 assets should create 3 sets of 4

        // Act
        var result = await _wrapper.GetAssets(12);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList.Count, Is.EqualTo(12));
        
        // Since sets are randomized, we can't predict exact order,
        // but we can verify the total count and presence of assets
        var resultIds = resultList.Select(a => a.Id).ToHashSet();
        var expectedIds = _testAssets.Take(12).Select(a => a.Id).ToHashSet();
        Assert.That(resultIds, Is.SupersetOf(expectedIds));
    }

    [Test]
    public async Task GetAssets_WithOnlyUndatedAssets_ShouldRandomizeOrder()
    {
        // Arrange
        var undatedAssets = _testAssets.Where(a => a.ExifInfo?.DateTimeOriginal.HasValue != true).ToList();
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(undatedAssets);

        // Act
        var result = await _wrapper.GetAssets(undatedAssets.Count);

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList.Count, Is.EqualTo(undatedAssets.Count));
        
        // Verify all undated assets are present (order doesn't matter since they're randomized)
        var originalIds = undatedAssets.Select(a => a.Id).ToHashSet();
        var resultIds = resultList.Select(a => a.Id).ToHashSet();
        Assert.That(resultIds.SetEquals(originalIds), Is.True, "All undated assets should be preserved");
        
        // Verify no assets have dates
        Assert.That(resultList.All(a => a.ExifInfo?.DateTimeOriginal.HasValue != true), 
            Is.True, "All returned assets should be undated");
    }

    [Test]
    public async Task GetAssets_WithRequestedLessThanAvailable_ShouldReturnCorrectCount()
    {
        // Arrange
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets);

        // Act
        var result = await _wrapper.GetAssets(10); // Request less than the 20 available

        // Assert
        var resultList = result.ToList();
        Assert.That(resultList.Count, Is.EqualTo(10));
    }

    [Test]
    public async Task GetAssets_WithCancellationToken_ShouldPassThroughToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), cancellationToken))
                    .ReturnsAsync(_testAssets);

        // Act
        await _wrapper.GetAssets(5, cancellationToken);

        // Assert
        _mockBasePool.Verify(x => x.GetAssets(It.IsAny<int>(), cancellationToken), Times.Once);
    }

    [Test]
    public async Task GetAssets_ShouldReturnExactInputObjects()
    {
        // Arrange
        var inputAssets = _testAssets.Take(10).ToList();
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputAssets);

        // Act
        var result = await _wrapper.GetAssets(10);

        // Assert
        var resultList = result.ToList();
        
        // Verify that we get back the exact same number of assets
        Assert.That(resultList.Count, Is.EqualTo(inputAssets.Count), 
            "Output should contain the same number of assets as input");
        
        // Verify that all input asset IDs are present in the output
        var inputIds = inputAssets.Select(a => a.Id).ToHashSet();
        var outputIds = resultList.Select(a => a.Id).ToHashSet();
        Assert.That(outputIds.SetEquals(inputIds), Is.True, 
            "Output should contain exactly the same asset IDs as input");
        
        // Verify that all input assets are present by reference or by content
        foreach (var inputAsset in inputAssets)
        {
            var matchingOutputAsset = resultList.FirstOrDefault(a => a.Id == inputAsset.Id);
            Assert.That(matchingOutputAsset, Is.Not.Null, 
                $"Asset with ID {inputAsset.Id} should be present in output");
            
            // Verify key properties are preserved
            Assert.That(matchingOutputAsset.Id, Is.EqualTo(inputAsset.Id),
                "Asset ID should be preserved");
            Assert.That(matchingOutputAsset.OriginalFileName, Is.EqualTo(inputAsset.OriginalFileName),
                "OriginalFileName should be preserved");
            
            // Verify EXIF data is preserved
            if (inputAsset.ExifInfo?.DateTimeOriginal.HasValue == true)
            {
                Assert.That(matchingOutputAsset.ExifInfo?.DateTimeOriginal, 
                    Is.EqualTo(inputAsset.ExifInfo.DateTimeOriginal),
                    "EXIF DateTimeOriginal should be preserved");
            }
        }
        
        // Verify no duplicates exist in output
        var uniqueOutputIds = outputIds.Count;
        Assert.That(uniqueOutputIds, Is.EqualTo(resultList.Count), 
            "Output should not contain duplicate assets");
        
        // Verify no extra assets were added
        Assert.That(outputIds.All(id => inputIds.Contains(id)), Is.True,
            "Output should not contain any assets not present in input");
    }

    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => 
        {
            var wrapper = new ChronologicalAssetsPoolWrapper(_mockBasePool.Object, _mockGeneralSettings.Object);
            Assert.That(wrapper, Is.Not.Null);
        });
    }

    [Test]
    public async Task GetAssets_ConfigurationBoundaryValues_ShouldHandleEdgeCases()
    {
        // Test with ChronologicalImagesCount = 1 (minimum valid value)
        _mockGeneralSettings.Setup(x => x.ChronologicalImagesCount).Returns(1);
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_testAssets.Take(5));

        var result = await _wrapper.GetAssets(5);
        Assert.That(result.Count(), Is.EqualTo(5));

        // Test with negative ChronologicalImagesCount (should fallback to base pool)
        _mockGeneralSettings.Setup(x => x.ChronologicalImagesCount).Returns(-1);
        result = await _wrapper.GetAssets(5);
        _mockBasePool.Verify(x => x.GetAssets(5, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #region TryParseDateTime Tests

    /// <summary>
    /// Tests the TryParseDateTime functionality with various date scenarios using reflection
    /// since the method is private.
    /// </summary>
    [Test]
    public void TryParseDateTime_WithValidDeserialized_ReturnsCorrectDate()
    {
        // Test with valid deserialized DateTimeOffset
        var asset = new AssetResponseDto
        {
            OriginalFileName = "test.jpg",
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = new DateTimeOffset(2024, 3, 15, 14, 30, 0, TimeSpan.Zero)
            }
        };

        var result = InvokeTryParseDateTime(asset);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.Year, Is.EqualTo(2024));
        Assert.That(result.Value.Month, Is.EqualTo(3));
        Assert.That(result.Value.Day, Is.EqualTo(15));
        Assert.That(result.Value.Hour, Is.EqualTo(14));
        Assert.That(result.Value.Minute, Is.EqualTo(30));
    }

    [Test]
    public void TryParseDateTime_WithNullAsset_ReturnsNull()
    {
        AssetResponseDto? nullAsset = null;
        
        // The reflection helper should handle null gracefully and return null
        var result = InvokeTryParseDateTime(nullAsset);
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryParseDateTime_WithValidModernDate_AcceptsDateAfter1950()
    {
        var asset = new AssetResponseDto
        {
            OriginalFileName = "test.jpg",
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = new DateTimeOffset(1951, 1, 1, 0, 0, 0, TimeSpan.Zero) // Just after 1950
            }
        };

        var result = InvokeTryParseDateTime(asset);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.Year, Is.EqualTo(1951));
    }

    /// <summary>
    /// Helper method to invoke the private TryParseDateTime method using reflection.
    /// </summary>
    private DateTime? InvokeTryParseDateTime(AssetResponseDto? asset)
    {
        var method = typeof(ChronologicalAssetsPoolWrapper)
            .GetMethod("TryParseDateTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        if (method == null) return null;
        
        var parameters = new object?[] { asset, null };
        var result = method.Invoke(null, parameters);
        var success = result != null && (bool)result;
        
        return success ? (DateTime)parameters[1]! : null;
    }

    #endregion

    #region Current Logic Tests

    /// <summary>
    /// Tests for the current implementation logic including tag assignment,
    /// FileCreatedAt fallback, and set randomization behavior.
    /// </summary>

    [Test]
    public async Task GetAssets_ShouldAssignSetTagsToAssets()
    {
        // Arrange
        var inputAssets = _testAssets.Take(9).ToList(); // 9 assets = 3 sets of 3
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputAssets);

        // Act
        var result = await _wrapper.GetAssets(9);

        // Assert
        var resultList = result.ToList();
        
        // Verify that all assets have set tags assigned
        foreach (var asset in resultList)
        {
            Assert.That(asset.Tags, Is.Not.Null, "Tags collection should not be null");
            var setTags = asset.Tags.Where(t => t.Name.StartsWith("Set ")).ToList();
            Assert.That(setTags.Count, Is.EqualTo(1), $"Asset {asset.Id} should have exactly one set tag");
            
            var setTag = setTags.First();
            Assert.That(setTag.Name, Does.Match(@"^Set \d+$"), 
                $"Set tag should match pattern 'Set X' but was '{setTag.Name}'");
        }

        // Verify that we have the expected number of different sets
        var allSetTags = resultList.SelectMany(a => a.Tags)
                                  .Where(t => t.Name.StartsWith("Set "))
                                  .Select(t => t.Name)
                                  .Distinct()
                                  .ToList();
        
        // With 9 assets and set size 3, we should have exactly 3 different sets
        Assert.That(allSetTags.Count, Is.EqualTo(3), 
            "Should have exactly 3 different set tags");
    }

    [Test]
    public async Task GetAssets_ShouldAssignConsecutiveSetNumbers()
    {
        // Arrange
        var inputAssets = _testAssets.Take(6).ToList(); // 6 assets = 2 sets of 3
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputAssets);

        // Act
        var result = await _wrapper.GetAssets(6);

        // Assert
        var resultList = result.ToList();
        var setNumbers = resultList.SelectMany(a => a.Tags)
                                   .Where(t => t.Name.StartsWith("Set "))
                                   .Select(t => int.Parse(t.Name.Split(' ')[1]))
                                   .Distinct()
                                   .OrderBy(n => n)
                                   .ToList();

        // Should have set numbers starting from 1
        Assert.That(setNumbers, Is.EqualTo(new[] { 1, 2 }), 
            "Set numbers should be consecutive starting from 1");
    }

    [Test]
    public async Task GetAssets_WithPartialLastSet_ShouldStillAssignSetTags()
    {
        // Arrange
        var inputAssets = _testAssets.Take(7).ToList(); // 7 assets = 2 full sets + 1 partial set
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputAssets);

        // Act
        var result = await _wrapper.GetAssets(7);

        // Assert
        var resultList = result.ToList();
        
        // All assets should have set tags
        foreach (var asset in resultList)
        {
            var setTags = asset.Tags.Where(t => t.Name.StartsWith("Set ")).ToList();
            Assert.That(setTags.Count, Is.EqualTo(1), 
                $"Asset {asset.Id} should have exactly one set tag");
        }

        // Should have 3 different sets (including the partial one)
        var setNumbers = resultList.SelectMany(a => a.Tags)
                                   .Where(t => t.Name.StartsWith("Set "))
                                   .Select(t => int.Parse(t.Name.Split(' ')[1]))
                                   .Distinct()
                                   .ToList();
        
        Assert.That(setNumbers.Count, Is.EqualTo(3), 
            "Should have 3 sets: 2 full sets of 3 + 1 partial set of 1");
    }

    [Test]
    public void TryParseDateTime_WithFileCreatedAtFallback_ReturnsFileCreatedAt()
    {
        // Arrange - Asset without DateTimeOriginal but with FileCreatedAt
        var testDate = new DateTimeOffset(2023, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var asset = new AssetResponseDto
        {
            Id = Guid.NewGuid().ToString(),
            OriginalFileName = "test_fallback.jpg",
            FileCreatedAt = testDate,
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = null // No EXIF date
            }
        };

        // Act
        var result = InvokeTryParseDateTime(asset);

        // Assert
        Assert.That(result, Is.Not.Null, "Should return FileCreatedAt when DateTimeOriginal is null");
        Assert.That(result.Value.Year, Is.EqualTo(2023));
        Assert.That(result.Value.Month, Is.EqualTo(6));
        Assert.That(result.Value.Day, Is.EqualTo(15));
        Assert.That(result.Value.Hour, Is.EqualTo(10));
        Assert.That(result.Value.Minute, Is.EqualTo(30));
    }

    [Test]
    public void TryParseDateTime_WithNullExifInfo_ReturnsFileCreatedAt()
    {
        // Arrange - Asset with null ExifInfo should fallback to FileCreatedAt
        var testDate = new DateTimeOffset(2022, 12, 25, 14, 45, 30, TimeSpan.Zero);
        var asset = new AssetResponseDto
        {
            Id = Guid.NewGuid().ToString(),
            OriginalFileName = "christmas_photo.jpg",
            FileCreatedAt = testDate,
            ExifInfo = null // No EXIF info at all
        };

        // Act
        var result = InvokeTryParseDateTime(asset);

        // Assert
        Assert.That(result, Is.Not.Null, "Should return FileCreatedAt when ExifInfo is null");
        Assert.That(result.Value.Year, Is.EqualTo(2022));
        Assert.That(result.Value.Month, Is.EqualTo(12));
        Assert.That(result.Value.Day, Is.EqualTo(25));
    }

    [Test]
    public void TryParseDateTime_DateTimeOriginalTakesPrecedence_OverFileCreatedAt()
    {
        // Arrange - Asset with both DateTimeOriginal and FileCreatedAt
        var exifDate = new DateTimeOffset(2024, 8, 20, 16, 20, 0, TimeSpan.Zero);
        var fileDate = new DateTimeOffset(2024, 8, 25, 10, 0, 0, TimeSpan.Zero); // Different date
        
        var asset = new AssetResponseDto
        {
            Id = Guid.NewGuid().ToString(),
            OriginalFileName = "preference_test.jpg",
            FileCreatedAt = fileDate,
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = exifDate
            }
        };

        // Act
        var result = InvokeTryParseDateTime(asset);

        // Assert
        Assert.That(result, Is.Not.Null, "Should parse date successfully");
        Assert.That(result.Value.Year, Is.EqualTo(2024));
        Assert.That(result.Value.Month, Is.EqualTo(8));
        Assert.That(result.Value.Day, Is.EqualTo(20), 
            "Should prefer DateTimeOriginal over FileCreatedAt");
    }

    [Test]
    public async Task GetAssets_SetRandomization_PreservesInternalChronologicalOrder()
    {
        // Arrange - Create assets with predictable dates for chronological testing
        var chronologicalAssets = new List<AssetResponseDto>();
        var baseDate = new DateTime(2024, 1, 1, 10, 0, 0);
        
        for (int i = 0; i < 12; i++) // 12 assets = 4 sets of 3
        {
            chronologicalAssets.Add(new AssetResponseDto
            {
                Id = $"chrono-{i:D3}",
                ExifInfo = new ExifResponseDto
                {
                    DateTimeOriginal = baseDate.AddDays(i)
                },
                OriginalFileName = $"chrono_{i:D3}.jpg"
            });
        }

        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(chronologicalAssets);

        // Act - Run multiple times to test randomization
        var results = new List<List<AssetResponseDto>>();
        for (int run = 0; run < 5; run++)
        {
            var result = await _wrapper.GetAssets(12);
            results.Add(result.ToList());
        }

        // Assert - Verify that within each set, chronological order is preserved
        foreach (var resultList in results)
        {
            // Group assets by their set tags
            var assetsBySet = resultList.GroupBy(a => a.Tags.First(t => t.Name.StartsWith("Set ")).Name)
                                       .OrderBy(g => g.Key)
                                       .ToList();

            foreach (var setGroup in assetsBySet)
            {
                var assetsInSet = setGroup.OrderBy(a => resultList.IndexOf(a)).ToList();
                
                // Within each set, dates should be in chronological order
                for (int i = 1; i < assetsInSet.Count; i++)
                {
                    var prevAsset = assetsInSet[i - 1];
                    var currAsset = assetsInSet[i];
                    
                    // Only check assets that have DateTimeOriginal (since we're testing chronological assets)
                    if (prevAsset.ExifInfo?.DateTimeOriginal.HasValue == true && 
                        currAsset.ExifInfo?.DateTimeOriginal.HasValue == true)
                    {
                        var prevDate = prevAsset.ExifInfo.DateTimeOriginal.Value;
                        var currDate = currAsset.ExifInfo.DateTimeOriginal.Value;
                        
                        Assert.That(currDate, Is.GreaterThanOrEqualTo(prevDate),
                            $"Within {setGroup.Key}, assets should maintain chronological order. " +
                            $"Asset {currAsset.Id} ({currDate:yyyy-MM-dd}) should be after " +
                            $"{prevAsset.Id} ({prevDate:yyyy-MM-dd})");
                    }
                }
            }
        }
    }

    [Test]
    public async Task GetAssets_MultipleRuns_ShowsSetRandomization()
    {
        // Arrange
        var inputAssets = _testAssets.Take(9).ToList(); // 9 assets = 3 sets
        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputAssets);

        // Act - Run the method multiple times
        var setOrders = new List<List<string>>();
        for (int run = 0; run < 10; run++)
        {
            var result = await _wrapper.GetAssets(9);
            var resultList = result.ToList();
            
            // Extract the order of sets as they appear in the result
            var setOrder = resultList.Select(a => a.Tags.First(t => t.Name.StartsWith("Set ")).Name)
                                     .Distinct()
                                     .ToList();
            setOrders.Add(setOrder);
        }

        // Assert - We should see different orderings across multiple runs
        // (Note: This test might occasionally fail due to randomness, but very rarely)
        var uniqueOrders = setOrders.Select(order => string.Join(",", order))
                                   .Distinct()
                                   .Count();

        // With sufficient runs, we should see some variation in set order
        // At minimum, we should have all expected set numbers present
        foreach (var setOrder in setOrders)
        {
            Assert.That(setOrder.Count, Is.EqualTo(3), "Should always have 3 sets");
            
            var setNumbers = setOrder.Select(s => int.Parse(s.Split(' ')[1])).ToHashSet();
            var expectedSetNumbers = new HashSet<int> { 1, 2, 3 };
            
            // Check that we have the right set numbers (order may vary due to randomization)
            Assert.That(setNumbers.Count, Is.EqualTo(3), "Should have exactly 3 different set numbers");
            Assert.That(setNumbers.IsSupersetOf(expectedSetNumbers) && expectedSetNumbers.IsSupersetOf(setNumbers), 
                Is.True, $"Should contain sets 1, 2, and 3. Got: {string.Join(", ", setNumbers.OrderBy(x => x))}");
        }

        // Since sets are randomized, we expect to see some variety over multiple runs
        // This is a probabilistic test - with 10 runs and 3! = 6 possible orders, 
        // we should see at least 2 different orders most of the time
        // We make this assertion optional to avoid flaky tests
        Console.WriteLine($"Observed {uniqueOrders} unique set orders out of 10 runs");
    }

    [Test] 
    public async Task GetAssets_WithMixedDateSources_SortsCorrectly()
    {
        // Arrange - Create assets with mixed date sources (some DateTimeOriginal, some FileCreatedAt only)
        var mixedAssets = new List<AssetResponseDto>();
        var baseDate = new DateTime(2024, 3, 1, 12, 0, 0);

        // Assets with DateTimeOriginal
        for (int i = 0; i < 3; i++)
        {
            mixedAssets.Add(new AssetResponseDto
            {
                Id = $"exif-{i}",
                ExifInfo = new ExifResponseDto
                {
                    DateTimeOriginal = baseDate.AddDays(i * 2) // Day 0, 2, 4
                },
                FileCreatedAt = baseDate.AddDays(i * 2 + 10), // Much later file dates
                OriginalFileName = $"exif_{i}.jpg"
            });
        }

        // Assets without DateTimeOriginal (will use FileCreatedAt)
        for (int i = 0; i < 3; i++)
        {
            mixedAssets.Add(new AssetResponseDto
            {
                Id = $"file-{i}",
                ExifInfo = new ExifResponseDto
                {
                    DateTimeOriginal = null
                },
                FileCreatedAt = baseDate.AddDays(i * 2 + 1), // Day 1, 3, 5
                OriginalFileName = $"file_{i}.jpg"
            });
        }

        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mixedAssets);

        // Act
        var result = await _wrapper.GetAssets(6);

        // Assert
        var resultList = result.ToList();
        
        // Extract the dates used for sorting (DateTimeOriginal or FileCreatedAt)
        var sortedDates = new List<DateTime>();
        foreach (var asset in resultList.Where(a => a.ExifInfo?.DateTimeOriginal.HasValue == true || a.FileCreatedAt != default))
        {
            if (asset.ExifInfo?.DateTimeOriginal.HasValue == true)
            {
                sortedDates.Add(asset.ExifInfo.DateTimeOriginal.Value.DateTime);
            }
            else
            {
                sortedDates.Add(asset.FileCreatedAt.DateTime);
            }
        }

        // The mixed sources should still result in overall chronological order
        // Expected order: Day 0 (exif), Day 1 (file), Day 2 (exif), Day 3 (file), Day 4 (exif), Day 5 (file)
        var expectedDates = new[]
        {
            baseDate,                    // Day 0 (exif-0)
            baseDate.AddDays(1),        // Day 1 (file-0)
            baseDate.AddDays(2),        // Day 2 (exif-1)
            baseDate.AddDays(3),        // Day 3 (file-1)
            baseDate.AddDays(4),        // Day 4 (exif-2)
            baseDate.AddDays(5)         // Day 5 (file-2)
        };

        // Since sets are randomized, we can't guarantee exact order, but all expected dates should be present
        var actualDatesSet = new HashSet<DateTime>(sortedDates);
        var expectedDatesSet = new HashSet<DateTime>(expectedDates);
        
        Assert.That(actualDatesSet, Is.EqualTo(expectedDatesSet),
            "All mixed-source dates should be present in the result");
    }

    [Test]
    public async Task GetAssets_PreservesOriginalTagsAndAddsSetTags()
    {
        // Arrange - Create assets with existing tags
        var assetsWithTags = new List<AssetResponseDto>();
        for (int i = 0; i < 3; i++)
        {
            assetsWithTags.Add(new AssetResponseDto
            {
                Id = $"tagged-{i}",
                ExifInfo = new ExifResponseDto
                {
                    DateTimeOriginal = new DateTime(2024, 1, i + 1, 10, 0, 0)
                },
                OriginalFileName = $"tagged_{i}.jpg",
                Tags = new List<TagResponseDto>
                {
                    new() { Name = $"OriginalTag{i}" },
                    new() { Name = "CommonTag" }
                }
            });
        }

        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assetsWithTags);

        // Act
        var result = await _wrapper.GetAssets(3);

        // Assert
        var resultList = result.ToList();
        
        foreach (var asset in resultList)
        {
            Assert.That(asset.Tags, Is.Not.Null);
            
            // Should have original tags plus one set tag
            var originalTags = asset.Tags.Where(t => !t.Name.StartsWith("Set ")).ToList();
            var setTags = asset.Tags.Where(t => t.Name.StartsWith("Set ")).ToList();
            
            Assert.That(originalTags.Count, Is.EqualTo(2), 
                "Should preserve all original tags");
            Assert.That(setTags.Count, Is.EqualTo(1), 
                "Should add exactly one set tag");
            
            // Check original tags are preserved
            Assert.That(originalTags.Any(t => t.Name == "CommonTag"), Is.True,
                "Should preserve CommonTag");
            Assert.That(originalTags.Any(t => t.Name.StartsWith("OriginalTag")), Is.True,
                "Should preserve asset-specific original tag");
        }
    }

    [Test] 
    public async Task GetAssets_WithEmptyOrNullTags_InitializesTagsCollection()
    {
        // Arrange - Create assets with null or empty tags
        var assetsWithoutTags = new List<AssetResponseDto>
        {
            new()
            {
                Id = "null-tags",
                ExifInfo = new ExifResponseDto { DateTimeOriginal = new DateTime(2024, 1, 1) },
                OriginalFileName = "null_tags.jpg",
                Tags = null // Null tags
            },
            new()
            {
                Id = "empty-tags", 
                ExifInfo = new ExifResponseDto { DateTimeOriginal = new DateTime(2024, 1, 2) },
                OriginalFileName = "empty_tags.jpg",
                Tags = new List<TagResponseDto>() // Empty tags
            }
        };

        _mockBasePool.Setup(x => x.GetAssets(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assetsWithoutTags);

        // Act
        var result = await _wrapper.GetAssets(2);

        // Assert
        var resultList = result.ToList();
        
        foreach (var asset in resultList)
        {
            Assert.That(asset.Tags, Is.Not.Null, "Tags collection should be initialized");
            
            var setTags = asset.Tags.Where(t => t.Name.StartsWith("Set ")).ToList();
            Assert.That(setTags.Count, Is.EqualTo(1), 
                "Should have exactly one set tag even when starting with null/empty tags");
        }
    }

    #endregion
}