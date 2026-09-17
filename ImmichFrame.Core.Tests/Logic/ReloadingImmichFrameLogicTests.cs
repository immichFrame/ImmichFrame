using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic;

[TestFixture]
public class ReloadingImmichFrameLogicTests
{
    private class FakeSettingsProvider : ISettingsProvider
    {
        public IServerSettings Current { get; set; } = Mock.Of<IServerSettings>();
        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        public void Raise(bool accountsChanged) => SettingsChanged?.Invoke(this, new SettingsChangedEventArgs
        {
            NewSettings = Current,
            AccountsChanged = accountsChanged,
            GeneralChanged = true
        });
    }

    private class DisposableLogicMock : Mock<IImmichFrameLogic>
    {
        public DisposableLogicMock() : base(MockBehavior.Loose)
        {
            As<IDisposable>();
        }

        public bool IsDisposed
        {
            get
            {
                try
                {
                    As<IDisposable>().Verify(d => d.Dispose(), Times.AtLeastOnce);
                    return true;
                }
                catch (MockException)
                {
                    return false;
                }
            }
        }
    }

    private FakeSettingsProvider _provider;
    private List<DisposableLogicMock> _created;
    private ReloadingImmichFrameLogic _logic;

    [SetUp]
    public void Setup()
    {
        _provider = new FakeSettingsProvider();
        _created = new List<DisposableLogicMock>();
        _logic = new ReloadingImmichFrameLogic(_provider, () =>
        {
            var mock = new DisposableLogicMock();
            _created.Add(mock);
            return mock.Object;
        }, NullLogger<ReloadingImmichFrameLogic>.Instance, disposeGraceDelay: TimeSpan.Zero);
    }

    [TearDown]
    public void TearDown() => _logic.Dispose();

    [Test]
    public void Constructor_BuildsInnerOnce()
    {
        Assert.That(_created, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AccountsChanged_SwapsInnerAndDisposesOld()
    {
        _provider.Raise(accountsChanged: true);

        Assert.That(_created, Has.Count.EqualTo(2));

        await _logic.GetTotalAssets();
        _created[0].Verify(l => l.GetTotalAssets(), Times.Never);
        _created[1].Verify(l => l.GetTotalAssets(), Times.Once);

        // Old instance is disposed after the (zero) grace delay
        await WaitUntil(() => _created[0].IsDisposed);
    }

    [Test]
    public async Task GeneralOnlyChange_KeepsInner()
    {
        _provider.Raise(accountsChanged: false);

        Assert.That(_created, Has.Count.EqualTo(1));
        await _logic.GetTotalAssets();
        _created[0].Verify(l => l.GetTotalAssets(), Times.Once);
    }

    [Test]
    public void Dispose_DisposesCurrentInnerAndUnsubscribes()
    {
        _logic.Dispose();

        Assert.That(_created[0].IsDisposed, Is.True);

        // Raising after dispose must not rebuild
        _provider.Raise(accountsChanged: true);
        Assert.That(_created, Has.Count.EqualTo(1));
    }

    private static async Task WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
            {
                Assert.Fail("Condition was not met in time");
            }

            await Task.Delay(10);
        }
    }
}
