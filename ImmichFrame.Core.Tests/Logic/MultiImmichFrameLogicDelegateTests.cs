using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic;

[TestFixture]
public class MultiImmichFrameLogicDelegateTests
{
    private Mock<IAccountSelectionStrategy> _strategy;
    private Dictionary<IAccountSettings, Mock<IAccountImmichFrameLogic>> _logics;
    private Mock<IServerSettings> _serverSettings;

    [SetUp]
    public void Setup()
    {
        _strategy = new Mock<IAccountSelectionStrategy>();
        _logics = new Dictionary<IAccountSettings, Mock<IAccountImmichFrameLogic>>();
        _serverSettings = new Mock<IServerSettings>();
    }

    private MultiImmichFrameLogicDelegate CreateDelegate(params string?[] accountNames)
    {
        var accounts = accountNames.Select(name =>
        {
            var settings = new Mock<IAccountSettings>();
            settings.SetupGet(s => s.Name).Returns(name);
            settings.SetupGet(s => s.ImmichServerUrl).Returns("https://photos.example");
            var logic = new Mock<IAccountImmichFrameLogic>();
            logic.SetupGet(l => l.AccountSettings).Returns(settings.Object);
            _logics[settings.Object] = logic;
            return settings.Object;
        }).ToList();
        _serverSettings.SetupGet(s => s.Accounts).Returns(accounts);

        return new MultiImmichFrameLogicDelegate(_serverSettings.Object, settings => _logics[settings].Object,
            NullLogger<MultiImmichFrameLogicDelegate>.Instance, _ => _strategy.Object);
    }

    private Mock<IAccountImmichFrameLogic> LogicNamed(string name) =>
        _logics.Single(entry => entry.Key.Name == name).Value;

    [Test]
    public async Task GetAssets_WithAccount_UsesOnlyThatAccountIgnoringCase()
    {
        var logic = CreateDelegate("family", "holidays");
        var holidays = LogicNamed("holidays");
        var asset = new AssetResponseDto { Id = Guid.NewGuid() };
        var selection = new AssetSelection("HOLIDAYS");
        _strategy.Setup(s => s.GetAssets(holidays.Object, selection))
            .ReturnsAsync(new[] { (holidays.Object, asset) });

        var result = (await logic.GetAssets(selection)).ToList();

        Assert.That(result.Select(a => a.Id), Is.EqualTo(new[] { asset.Id }));
        _strategy.Verify(s => s.GetAssets(), Times.Never);
    }

    [Test]
    public async Task GetAssets_UnknownAccount_ReturnsNothing()
    {
        var logic = CreateDelegate("family", null);

        var result = await logic.GetAssets(new AssetSelection("unknown"));

        Assert.That(result, Is.Empty);
        _strategy.Verify(s => s.GetAssets(), Times.Never);
        _strategy.Verify(s => s.GetAssets(It.IsAny<IAccountImmichFrameLogic>(), It.IsAny<AssetSelection>()), Times.Never);
    }
}
