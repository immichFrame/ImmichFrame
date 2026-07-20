using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ImmichFrame.Core.Events;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models.Events;
using ImmichFrame.WebApi.Services;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Events;

[TestFixture]
public class FrameEventValidatorTests
{
    private FrameEventValidator _validator;

    [SetUp]
    public void SetUp()
    {
        var settings = new Mock<IGeneralSettings>();
        settings.Setup(s => s.EventDefaultTimeoutMs).Returns(15000);
        _validator = new FrameEventValidator(settings.Object);
    }

    private static FrameEventRequestDto MakeValidPopupText()
    {
        return new FrameEventRequestDto
        {
            DeviceId = "device-1",
            Id = "evt-1",
            Type = "frame.ui.v1",
            Mode = FrameEventMode.PopupText,
            Message = "Hello world"
        };
    }

    private static FrameEventRequestDto MakeValidBanner()
    {
        return new FrameEventRequestDto
        {
            DeviceId = "device-1",
            Id = "evt-banner-1",
            Type = "frame.ui.banner",
            Mode = FrameEventMode.Banner,
            Message = "banner text"
        };
    }

    [Test]
    public void Validate_NullDto_Throws()
    {
        Assert.Throws<ValidationException>(() => _validator.Validate(null!));
    }

    [Test]
    public void Validate_Succeeds_ForValidPopupText()
    {
        var result = _validator.Validate(MakeValidPopupText());
        Assert.That(result.Id, Is.EqualTo("evt-1"));
        Assert.That(result.Mode, Is.EqualTo(FrameEventMode.PopupText));
        Assert.That(result.Message, Is.EqualTo("Hello world"));
    }

    [Test]
    public void Validate_Throws_WhenDeviceIdMissing()
    {
        var dto = MakeValidPopupText();
        dto.DeviceId = "";
        Assert.Throws<ValidationException>(() => _validator.Validate(dto));
    }

    [Test]
    public void Validate_Throws_WhenIdMissing()
    {
        var dto = MakeValidPopupText();
        dto.Id = "";
        Assert.Throws<ValidationException>(() => _validator.Validate(dto));
    }

    [Test]
    public void Validate_Throws_WhenTypeMissing()
    {
        var dto = MakeValidPopupText();
        dto.Type = "";
        Assert.Throws<ValidationException>(() => _validator.Validate(dto));
    }

    [Test]
    public void Validate_Throws_WhenTypeInvalid()
    {
        var dto = MakeValidPopupText();
        dto.Type = "invalid.type";
        Assert.Throws<ValidationException>(() => _validator.Validate(dto));
    }

    [Test]
    public void Validate_Throws_ForPopupTextWithoutMessage()
    {
        var dto = MakeValidPopupText();
        dto.Message = null;
        Assert.Throws<ValidationException>(() => _validator.Validate(dto));
    }

    [Test]
    public void Validate_UsesDefaultTimeout_WhenNotProvided()
    {
        var dto = MakeValidPopupText();
        dto.TimeoutMs = null;

        var result = _validator.Validate(dto);
        Assert.That(result.TimeoutMs, Is.EqualTo(15000));
    }

    [Test]
    public void Validate_Throws_WhenTimeoutNegative()
    {
        var dto = MakeValidPopupText();
        dto.TimeoutMs = -1;
        Assert.Throws<ValidationException>(() => _validator.Validate(dto));
    }

    [Test]
    public void Validate_Succeeds_ForCloseMode()
    {
        var dto = new FrameEventRequestDto
        {
            DeviceId = "device-1",
            Id = "close-1",
            Type = "frame.ui.v1",
            Mode = FrameEventMode.Close,
            Category = "alerts"
        };

        var result = _validator.Validate(dto);
        Assert.That(result.Mode, Is.EqualTo(FrameEventMode.Close));
    }

    [Test]
    public void Validate_BannerWithMessage_Succeeds()
    {
        var dto = MakeValidBanner();

        var domain = _validator.Validate(dto);

        Assert.That(domain.Mode, Is.EqualTo(FrameEventMode.Banner));
        Assert.That(domain.Message, Is.EqualTo("banner text"));
    }

    [Test]
    public void Validate_BannerWithoutMessage_Throws()
    {
        var dto = MakeValidBanner();
        dto.Message = null;

        Assert.Throws<ValidationException>(() => _validator.Validate(dto));
    }

    [Test]
    public void Validate_BannerWithTitleAndActions_StillValidates()
    {
        var dto = MakeValidBanner();
        dto.Title = "banner title";
        dto.Actions = new List<FrameEventActionDto>
        {
            new() { Id = "ack", Label = "OK", Kind = "primary" }
        };

        var domain = _validator.Validate(dto);

        Assert.That(domain.Title, Is.EqualTo("banner title"));
        Assert.That(domain.Actions, Has.Count.EqualTo(1));
    }
}
