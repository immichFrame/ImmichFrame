using NUnit.Framework;
using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Tests.Models;

[TestFixture]
public class TemperatureDecimalDigitsValidationTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void GeneralSettings_AcceptsSupportedPrecision(int digits)
    {
        var settings = new GeneralSettings { TemperatureDecimalDigits = digits };

        Assert.DoesNotThrow(() => settings.Validate());
    }

    [TestCase(-1)]
    [TestCase(3)]
    [TestCase(101)]
    public void GeneralSettings_RejectsUnsupportedPrecision(int digits)
    {
        var settings = new GeneralSettings { TemperatureDecimalDigits = digits };

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void V1Config_AcceptsSupportedPrecision(int digits)
    {
        var v1 = new ServerSettingsV1 { TemperatureDecimalDigits = digits };

        Assert.DoesNotThrow(() => new ServerSettingsV1Adapter(v1).GeneralSettings.Validate());
    }

    [TestCase(-1)]
    [TestCase(101)]
    public void V1Config_RejectsUnsupportedPrecision(int digits)
    {
        var v1 = new ServerSettingsV1 { TemperatureDecimalDigits = digits };

        Assert.That(() => new ServerSettingsV1Adapter(v1).GeneralSettings.Validate(),
            Throws.TypeOf<ArgumentOutOfRangeException>(),
            "a legacy config must not pass through a value that would throw RangeError in toFixed()");
    }
}
