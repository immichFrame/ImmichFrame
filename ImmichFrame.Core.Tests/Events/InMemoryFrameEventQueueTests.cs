using ImmichFrame.Core.Events;
using ImmichFrame.Core.Services;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Events;

[TestFixture]
public class InMemoryFrameEventQueueTests
{
    private InMemoryFrameEventQueue _queue;

    [SetUp]
    public void SetUp()
    {
        _queue = new InMemoryFrameEventQueue();
    }

    private static FrameEvent MakeEvent(FrameEventMode mode, string id, string deviceId = "device-1", int priority = 0, string? category = null, int? timeoutMs = null)
    {
        return new FrameEvent
        {
            Id = id,
            DeviceId = deviceId,
            Type = "frame.ui.v1",
            Mode = mode,
            Message = $"Message for {id}",
            Priority = priority,
            Category = category,
            TimeoutMs = timeoutMs,
            PostedAt = DateTime.UtcNow
        };
    }

    private static FrameEvent MakeEvent(string id, string deviceId = "device-1", int priority = 0, string? category = null, int? timeoutMs = null)
    {
        return new FrameEvent
        {
            Id = id,
            DeviceId = deviceId,
            Type = "frame.ui.v1",
            Mode = FrameEventMode.PopupText,
            Message = $"Message for {id}",
            Priority = priority,
            Category = category,
            TimeoutMs = timeoutMs,
            PostedAt = DateTime.UtcNow
        };
    }

    [Test]
    public async Task PeekNextAsync_ReturnsNull_WhenQueueEmpty()
    {
        var result = await _queue.PeekNextAsync("device-1");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task PeekNextAsync_ReturnsHighestPriority()
    {
        await _queue.EnqueueAsync(MakeEvent("low", priority: 10));
        await _queue.EnqueueAsync(MakeEvent("high", priority: 1));

        var result = await _queue.PeekNextAsync("device-1");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("high"));
    }

    [Test]
    public async Task EnqueueAsync_ReturnsFalse_WhenDuplicateId()
    {
        var first = await _queue.EnqueueAsync(MakeEvent("dup"));
        var second = await _queue.EnqueueAsync(MakeEvent("dup"));

        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
    }

    [Test]
    public async Task EnqueueAsync_ReplacesCategory()
    {
        await _queue.EnqueueAsync(MakeEvent("old", category: "alerts"));
        await _queue.EnqueueAsync(MakeEvent("new", category: "alerts"));

        var result = await _queue.PeekNextAsync("device-1");
        Assert.That(result!.Id, Is.EqualTo("new"));

        var snapshot = _queue.GetDeviceSnapshot("device-1");
        Assert.That(snapshot, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task EnqueueAsync_CloseMode_RemovesMatchingCategory()
    {
        await _queue.EnqueueAsync(MakeEvent("alert1", category: "alerts"));
        await _queue.EnqueueAsync(MakeEvent("other", category: "info"));

        var closeEvent = new FrameEvent
        {
            Id = "close-1",
            DeviceId = "device-1",
            Type = "frame.ui.v1",
            Mode = FrameEventMode.Close,
            Category = "alerts",
            PostedAt = DateTime.UtcNow
        };
        await _queue.EnqueueAsync(closeEvent);

        var snapshot = _queue.GetDeviceSnapshot("device-1");
        Assert.That(snapshot, Has.Count.EqualTo(1));
        Assert.That(snapshot[0].Event.Id, Is.EqualTo("other"));
    }

    [Test]
    public async Task EnqueueAsync_CloseModeWithoutCategory_RemovesAll()
    {
        await _queue.EnqueueAsync(MakeEvent("a"));
        await _queue.EnqueueAsync(MakeEvent("b"));

        var closeEvent = new FrameEvent
        {
            Id = "close-all",
            DeviceId = "device-1",
            Type = "frame.ui.v1",
            Mode = FrameEventMode.Close,
            PostedAt = DateTime.UtcNow
        };
        await _queue.EnqueueAsync(closeEvent);

        var result = await _queue.PeekNextAsync("device-1");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AckAsync_DoesNotRemove_OnShown()
    {
        await _queue.EnqueueAsync(MakeEvent("evt"));
        await _queue.PeekNextAsync("device-1");

        var acked = await _queue.AckAsync("device-1", "evt", FrameEventAckStatus.Shown);
        Assert.That(acked, Is.True);

        var result = await _queue.PeekNextAsync("device-1");
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task AckAsync_Removes_OnClosed()
    {
        await _queue.EnqueueAsync(MakeEvent("evt"));
        await _queue.PeekNextAsync("device-1");

        var acked = await _queue.AckAsync("device-1", "evt", FrameEventAckStatus.Closed);
        Assert.That(acked, Is.True);

        var result = await _queue.PeekNextAsync("device-1");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AckAsync_ReturnsFalse_WhenEventNotFound()
    {
        var result = await _queue.AckAsync("device-1", "nonexistent", FrameEventAckStatus.Closed);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetDeviceSnapshot_ReturnsEmpty_ForUnknownDevice()
    {
        var snapshot = _queue.GetDeviceSnapshot("unknown");
        Assert.That(snapshot, Is.Empty);
    }

    [Test]
    public async Task PeekNext_WithModeFilter_ReturnsOnlyMatchingMode()
    {
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.PopupText, "popup-1"));
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.Banner, "banner-1"));

        var popup = await _queue.PeekNextAsync("device-1", FrameEventMode.PopupText);
        var banner = await _queue.PeekNextAsync("device-1", FrameEventMode.Banner);

        Assert.That(popup, Is.Not.Null);
        Assert.That(popup!.Id, Is.EqualTo("popup-1"));
        Assert.That(banner, Is.Not.Null);
        Assert.That(banner!.Id, Is.EqualTo("banner-1"));
    }

    [Test]
    public async Task PeekNext_WithModeFilter_ReturnsNullWhenNoMatch()
    {
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.PopupText, "popup-1"));

        var banner = await _queue.PeekNextAsync("device-1", FrameEventMode.Banner);

        Assert.That(banner, Is.Null);
    }

    [Test]
    public async Task PeekNext_AfterAckingPopup_BannerStillReturned()
    {
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.PopupText, "popup-1"));
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.Banner, "banner-1"));

        await _queue.AckAsync("device-1", "popup-1", FrameEventAckStatus.Closed);

        var banner = await _queue.PeekNextAsync("device-1", FrameEventMode.Banner);
        Assert.That(banner, Is.Not.Null);
        Assert.That(banner!.Id, Is.EqualTo("banner-1"));
    }

    [Test]
    public async Task Enqueue_BannerWithCategory_DoesNotEvictPopupWithSameCategory()
    {
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.PopupText, "popup-1", category: "shared"));
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.Banner, "banner-1", category: "shared"));

        var popup = await _queue.PeekNextAsync("device-1", FrameEventMode.PopupText);
        var banner = await _queue.PeekNextAsync("device-1", FrameEventMode.Banner);

        Assert.That(popup, Is.Not.Null);
        Assert.That(popup!.Id, Is.EqualTo("popup-1"));
        Assert.That(banner, Is.Not.Null);
        Assert.That(banner!.Id, Is.EqualTo("banner-1"));
    }

    [Test]
    public async Task Enqueue_BannerWithCategory_ReplacesOlderBannerWithSameCategory()
    {
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.Banner, "banner-old", category: "shared"));
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.Banner, "banner-new", category: "shared"));

        var snapshot = _queue.GetDeviceSnapshot("device-1");
        Assert.That(snapshot.Count, Is.EqualTo(1));
        Assert.That(snapshot[0].Event.Id, Is.EqualTo("banner-new"));
    }

    [Test]
    public async Task PeekNext_NoModeFilter_ReturnsHighestPriorityRegardlessOfMode()
    {
        // The EventEntryComparer sorts ascending by priority (lower numeric value = higher effective priority).
        // popup-low gets priority 10 (low effective priority), banner-high gets priority 0 (high effective priority).
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.PopupText, "popup-low", priority: 10));
        await _queue.EnqueueAsync(MakeEvent(FrameEventMode.Banner, "banner-high", priority: 0));

        var top = await _queue.PeekNextAsync("device-1");

        Assert.That(top, Is.Not.Null);
        Assert.That(top!.Id, Is.EqualTo("banner-high"));
    }
}
