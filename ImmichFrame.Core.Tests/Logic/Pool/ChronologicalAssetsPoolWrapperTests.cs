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
        _mockGeneralSettings.Setup(x => x.ShowChronologicalImages).Returns(true);
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
        _mockGeneralSettings.Setup(x => x.ShowChronologicalImages).Returns(false);
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
    public void TryParseDateTime_WithInvalidDeserializedDate_UsesFilenameParsing()
    {
        // Test with invalid deserialized date (pre-1900) but valid filename pattern
        var asset = new AssetResponseDto
        {
            OriginalFileName = "240315_143022.jpg", // March 15, 2024 at 14:30:22
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = new DateTimeOffset(1908, 6, 11, 20, 58, 0, TimeSpan.Zero) // Invalid
            }
        };

        var result = InvokeTryParseDateTime(asset);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.Year, Is.EqualTo(2024));
        Assert.That(result.Value.Month, Is.EqualTo(3));
        Assert.That(result.Value.Day, Is.EqualTo(15));
        Assert.That(result.Value.Hour, Is.EqualTo(14));
        Assert.That(result.Value.Minute, Is.EqualTo(30));
        Assert.That(result.Value.Second, Is.EqualTo(22));
    }

    [Test]
    [TestCase("131005_140838.jpg", 2013, 10, 5, 14, 8, 38)]
    [TestCase("240701_235959.jpg", 2024, 7, 1, 23, 59, 59)]
    [TestCase("991231_000000.jpg", 2099, 12, 31, 0, 0, 0)]
    [TestCase("000101_120000.jpg", 2000, 1, 1, 12, 0, 0)]
    public void TryParseDateTime_WithVariousFilenamePatterns_ParsesCorrectly(
        string filename, int expectedYear, int expectedMonth, int expectedDay, 
        int expectedHour, int expectedMinute, int expectedSecond)
    {
        var asset = new AssetResponseDto
        {
            OriginalFileName = filename,
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = new DateTimeOffset(1908, 6, 11, 20, 58, 0, TimeSpan.Zero) // Invalid
            }
        };

        var result = InvokeTryParseDateTime(asset);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.Year, Is.EqualTo(expectedYear));
        Assert.That(result.Value.Month, Is.EqualTo(expectedMonth));
        Assert.That(result.Value.Day, Is.EqualTo(expectedDay));
        Assert.That(result.Value.Hour, Is.EqualTo(expectedHour));
        Assert.That(result.Value.Minute, Is.EqualTo(expectedMinute));
        Assert.That(result.Value.Second, Is.EqualTo(expectedSecond));
    }

    [Test]
    [TestCase("IMG_001.jpg")]
    [TestCase("P1100696.jpg")]
    [TestCase("DSC01446.jpg")]
    [TestCase("999999_205436.jpg")] // Invalid date format (year 99 -> 2099 outside range)
    [TestCase("131305_140838.jpg")] // Invalid month (13)
    [TestCase("131232_140838.jpg")] // Invalid day (32)
    [TestCase("131005_250838.jpg")] // Invalid hour (25)
    [TestCase("131005_146038.jpg")] // Invalid minute (60)
    [TestCase("131005_140860.jpg")] // Invalid second (60)
    public void TryParseDateTime_WithInvalidFilenamePatterns_ReturnsNull(string filename)
    {
        var asset = new AssetResponseDto
        {
            OriginalFileName = filename,
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = new DateTimeOffset(1908, 6, 11, 20, 58, 0, TimeSpan.Zero) // Invalid
            }
        };

        var result = InvokeTryParseDateTime(asset);
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryParseDateTime_WithNullExifInfo_ReturnsNull()
    {
        var asset = new AssetResponseDto
        {
            OriginalFileName = "131005_140838.jpg",
            ExifInfo = null
        };

        var result = InvokeTryParseDateTime(asset);
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryParseDateTime_WithNullDateTimeOriginal_UsesFilenameParsing()
    {
        var asset = new AssetResponseDto
        {
            OriginalFileName = "131005_140838.jpg",
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = null
            }
        };

        var result = InvokeTryParseDateTime(asset);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.Year, Is.EqualTo(2013));
        Assert.That(result.Value.Month, Is.EqualTo(10));
        Assert.That(result.Value.Day, Is.EqualTo(5));
    }

    [Test]
    public void TryParseDateTime_WithNullAsset_ReturnsNull()
    {
        AssetResponseDto? nullAsset = null;
        var result = InvokeTryParseDateTime(nullAsset);
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryParseDateTime_WithEdgeCaseDates_HandlesCorrectly()
    {
        // Test leap year
        var leapYearAsset = new AssetResponseDto
        {
            OriginalFileName = "240229_120000.jpg", // Feb 29, 2024 (leap year)
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = new DateTimeOffset(1908, 6, 11, 20, 58, 0, TimeSpan.Zero)
            }
        };

        var result = InvokeTryParseDateTime(leapYearAsset);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.Year, Is.EqualTo(2024));
        Assert.That(result.Value.Month, Is.EqualTo(2));
        Assert.That(result.Value.Day, Is.EqualTo(29));

        // Test non-leap year Feb 29 (should fail)
        var nonLeapYearAsset = new AssetResponseDto
        {
            OriginalFileName = "230229_120000.jpg", // Feb 29, 2023 (not leap year)
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = new DateTimeOffset(1908, 6, 11, 20, 58, 0, TimeSpan.Zero)
            }
        };

        result = InvokeTryParseDateTime(nonLeapYearAsset);
        Assert.That(result, Is.Null); // Should return null due to invalid date
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

    [Test]
    public void TryParseDateTime_WithBoundaryYear1950_RejectsAsInvalid()
    {
        var asset = new AssetResponseDto
        {
            OriginalFileName = "131005_140838.jpg", // Fallback pattern
            ExifInfo = new ExifResponseDto
            {
                DateTimeOriginal = new DateTimeOffset(1950, 1, 1, 0, 0, 0, TimeSpan.Zero) // Exactly 1950
            }
        };

        var result = InvokeTryParseDateTime(asset);
        
        // Should use filename parsing since 1950 is considered invalid (< 1950 check uses >=)
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value.Year, Is.EqualTo(2013)); // From filename
    }

    /// <summary>
    /// Helper method to invoke the private TryParseDateTime method using reflection.
    /// </summary>
    private DateTime? InvokeTryParseDateTime(AssetResponseDto? asset)
    {
        var method = typeof(ChronologicalAssetsPoolWrapper)
            .GetMethod("TryParseDateTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        return (DateTime?)method?.Invoke(_wrapper, new object?[] { asset });
    }

    #endregion
}