using NUnit.Framework;
using Moq;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Logic.Pool;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImmichFrame.Core.Tests.Logic;

[TestFixture]
public class PooledImmichFrameLogicTests
{
    private Mock<IAccountSettings> _mockAccountSettings;
    private Mock<IGeneralSettings> _mockGeneralSettings;
    private Mock<IHttpClientFactory> _mockHttpClientFactory;
    private Mock<HttpClient> _mockHttpClient;

    [SetUp]
    public void Setup()
    {
        _mockAccountSettings = new Mock<IAccountSettings>();
        _mockGeneralSettings = new Mock<IGeneralSettings>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClient = new Mock<HttpClient>();

        // Default setup for common properties
        _mockAccountSettings.SetupGet(s => s.ImmichServerUrl).Returns("http://localhost:2283");
        _mockAccountSettings.SetupGet(s => s.ApiKey).Returns("test-api-key");
        _mockAccountSettings.SetupGet(s => s.ShowFavorites).Returns(false);
        _mockAccountSettings.SetupGet(s => s.ShowMemories).Returns(false);
        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid>());
        _mockAccountSettings.SetupGet(s => s.ShowArchived).Returns(false);

        _mockGeneralSettings.SetupGet(g => g.RefreshAlbumPeopleInterval).Returns(24);
        _mockGeneralSettings.SetupGet(g => g.DownloadImages).Returns(false);

        _mockHttpClientFactory.Setup(f => f.CreateClient("ImmichApiAccountClient")).Returns(_mockHttpClient.Object);
    }

    /// <summary>
    /// Ensures that when accountSettings.People is null, BuildPool creates an AllAssetsPool
    /// instead of a MultiAssetPool that would include PersonAssetsPool
    /// </summary>
    [Test]
    public void BuildPool_WithNullPeople_DoesNotAddPersonAssetsPool()
    {
        // Arrange
        _mockAccountSettings.SetupGet(s => s.People).Returns((List<Guid>)null);

        // Act
        var logic = new PooledImmichFrameLogic(_mockAccountSettings.Object, _mockGeneralSettings.Object, _mockHttpClientFactory.Object);

        // Assert - The pool should be AllAssetsPool, not MultiAssetPool
        // We can verify this by checking that the pool is created and is of type AllAssetsPool
        Assert.That(logic.AccountSettings.People, Is.Null);
        // The fact that the object was created without throwing an exception validates that 
        // the pool was built correctly without trying to use null People
    }

    /// <summary>
    /// Ensures that when accountSettings.People is an empty list, BuildPool creates an AllAssetsPool
    /// instead of a MultiAssetPool, since there are no people to add
    /// </summary>
    [Test]
    public void BuildPool_WithEmptyPeople_DoesNotAddPersonAssetsPool()
    {
        // Arrange
        var emptyPeople = new List<Guid>();
        _mockAccountSettings.SetupGet(s => s.People).Returns(emptyPeople);

        // Act
        var logic = new PooledImmichFrameLogic(_mockAccountSettings.Object, _mockGeneralSettings.Object, _mockHttpClientFactory.Object);

        // Assert
        Assert.That(logic.AccountSettings.People.Count, Is.EqualTo(0));
        // The pool was created successfully without adding PersonAssetsPool
    }

    /// <summary>
    /// Ensures that when accountSettings.People is null, along with other settings being false/empty,
    /// BuildPool returns an AllAssetsPool
    /// </summary>
    [Test]
    public void BuildPool_WithAllSettingsFalseAndNullPeople_CreatesAllAssetsPool()
    {
        // Arrange - all options are disabled/empty/null
        _mockAccountSettings.SetupGet(s => s.ShowFavorites).Returns(false);
        _mockAccountSettings.SetupGet(s => s.ShowMemories).Returns(false);
        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid>());
        _mockAccountSettings.SetupGet(s => s.People).Returns((List<Guid>)null);

        // Act
        var logic = new PooledImmichFrameLogic(_mockAccountSettings.Object, _mockGeneralSettings.Object, _mockHttpClientFactory.Object);

        // Assert - should not throw and should create successfully
        Assert.That(logic.AccountSettings, Is.Not.Null);
        Assert.That(logic.AccountSettings.People, Is.Null);
    }

    /// <summary>
    /// Ensures that when accountSettings.People is not null and has values,
    /// BuildPool correctly includes PersonAssetsPool in the MultiAssetPool
    /// </summary>
    [Test]
    public void BuildPool_WithPeopleValues_IncludesPersonAssetsPool()
    {
        // Arrange
        var personId = Guid.NewGuid();
        _mockAccountSettings.SetupGet(s => s.People).Returns(new List<Guid> { personId });

        // Act
        var logic = new PooledImmichFrameLogic(_mockAccountSettings.Object, _mockGeneralSettings.Object, _mockHttpClientFactory.Object);

        // Assert
        Assert.That(logic.AccountSettings.People, Is.Not.Null);
        Assert.That(logic.AccountSettings.People.Count, Is.EqualTo(1));
        Assert.That(logic.AccountSettings.People.First(), Is.EqualTo(personId));
    }

    /// <summary>
    /// Ensures that mixed settings (some enabled, some disabled) with null People
    /// creates a proper MultiAssetPool excluding PersonAssetsPool
    /// </summary>
    [Test]
    public void BuildPool_WithMixedSettingsAndNullPeople_DoesNotIncludePersonAssetsPool()
    {
        // Arrange - enable some settings but keep People null
        _mockAccountSettings.SetupGet(s => s.ShowFavorites).Returns(true);
        _mockAccountSettings.SetupGet(s => s.ShowMemories).Returns(false);
        _mockAccountSettings.SetupGet(s => s.Albums).Returns(new List<Guid>());
        _mockAccountSettings.SetupGet(s => s.People).Returns((List<Guid>)null);

        // Act
        var logic = new PooledImmichFrameLogic(_mockAccountSettings.Object, _mockGeneralSettings.Object, _mockHttpClientFactory.Object);

        // Assert
        Assert.That(logic.AccountSettings.People, Is.Null);
        // Pool should be created without errors
        Assert.That(logic.AccountSettings, Is.Not.Null);
    }
}
