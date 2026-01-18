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
    private PooledImmichFrameLogic _logic;
    private List<Guid> people;
    private List<Guid> albums;

    [SetUp]
    public void Setup()
    {
        _mockAccountSettings = new Mock<IAccountSettings>();
        _mockGeneralSettings = new Mock<IGeneralSettings>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClient = new Mock<HttpClient>();

        // Mocks are not set up for other test cases as these tests were borne out of a
        // task to add null tolerance for accountSettings.People. When tests are added
        // for other PooledImmichFrameLogic features, the mock setups likely need to be revisited.

        people = new List<Guid>();
        _mockAccountSettings.SetupGet(s => s.People).Returns(() => people);

        albums = new List<Guid>();
        _mockAccountSettings.SetupGet(s => s.ExcludedAlbums).Returns(() => albums);

        _mockHttpClientFactory.Setup(f => f.CreateClient("ImmichApiAccountClient")).Returns(_mockHttpClient.Object);

        _logic = new PooledImmichFrameLogic(_mockAccountSettings.Object, _mockGeneralSettings.Object, _mockHttpClientFactory.Object);
    }

    /// <summary>
    /// Ensures that when accountSettings.People is null, BuildPool does not fail
    /// </summary>
    [Test]
    public void BuildPool_WithNullPeople_Succeeds()
    {
        // Arrange
        people = null;
        
        // The absence of an error during construction (because people information is loaded during construction)
        // is sufficient to validate that the code handles null people gracefully.
        Assert.DoesNotThrow(() => new PooledImmichFrameLogic(_mockAccountSettings.Object, _mockGeneralSettings.Object, _mockHttpClientFactory.Object));
    }

    [Test]
    public void BuildPool_WithNullAlbums_Succeeds()
    {
        albums = null;

        // The absence of an error during construction (because album information is loaded during construction)
        // is sufficient to validate that the code handles null albums gracefully.
        Assert.DoesNotThrow(() => new PooledImmichFrameLogic(_mockAccountSettings.Object, _mockGeneralSettings.Object, _mockHttpClientFactory.Object));
    }
}
