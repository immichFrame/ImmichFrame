using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Models;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Models;

[TestFixture]
public class EventSettingsBoundsTests
{
    [TestCase(-5, 1)]
    [TestCase(0, 1)]
    [TestCase(5, 5)]
    [TestCase(3601, 3600)]
    public void V2_EventPollingIntervalSeconds_IsClampedOnSet(int input, int expected)
    {
        var settings = new GeneralSettings { EventPollingIntervalSeconds = input };
        Assert.That(settings.EventPollingIntervalSeconds, Is.EqualTo(expected));
    }

    [TestCase(-1, 100)]
    [TestCase(50, 100)]
    [TestCase(500, 500)]
    [TestCase(400_000, 300_000)]
    public void V2_EventDefaultTimeoutMs_IsClampedOnSet(int input, int expected)
    {
        var settings = new GeneralSettings { EventDefaultTimeoutMs = input };
        Assert.That(settings.EventDefaultTimeoutMs, Is.EqualTo(expected));
    }

    [TestCase(0, 1)]
    [TestCase(10_000, 3600)]
    public void V1_EventPollingIntervalSeconds_IsClampedOnSet(int input, int expected)
    {
        var settings = new ServerSettingsV1 { EventPollingIntervalSeconds = input };
        Assert.That(settings.EventPollingIntervalSeconds, Is.EqualTo(expected));
    }

    [TestCase(0, 100)]
    [TestCase(10_000_000, 300_000)]
    public void V1_EventDefaultTimeoutMs_IsClampedOnSet(int input, int expected)
    {
        var settings = new ServerSettingsV1 { EventDefaultTimeoutMs = input };
        Assert.That(settings.EventDefaultTimeoutMs, Is.EqualTo(expected));
    }
}
