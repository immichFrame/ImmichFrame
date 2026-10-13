using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Models;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Helpers;

[TestFixture]
public class ImageContentTypesTests
{
    [TestCase("image/jpeg", "jpeg")]
    [TestCase("image/webp", "webp")]
    [TestCase("IMAGE/HEIC", "heic")]
    [TestCase("image/png; charset=binary", "png")]
    [TestCase("image/x-adobe-dng", "dng")]
    [TestCase("image/x-nikon-nef", "nikon-nef")]
    [TestCase("", "bin")]
    [TestCase(null, "bin")]
    public void ToExtension(string? contentType, string expected) =>
        Assert.That(ImageContentTypes.ToExtension(contentType), Is.EqualTo(expected));

    [TestCase("jpeg", "image/jpeg")]
    [TestCase(".heic", "image/heic")]
    [TestCase("WEBP", "image/webp")]
    [TestCase("nikon-nef", "application/octet-stream")]
    public void FromExtension(string extension, string expected) =>
        Assert.That(ImageContentTypes.FromExtension(extension), Is.EqualTo(expected));

    [TestCase("image/jpeg", true)]
    [TestCase("image/avif", true)]
    [TestCase("image/png; charset=binary", true)]
    [TestCase("image/heic", false)]
    [TestCase("image/x-adobe-dng", false)]
    [TestCase(null, false)]
    public void IsWebSupported(string? contentType, bool expected) =>
        Assert.That(ImageContentTypes.IsWebSupported(contentType), Is.EqualTo(expected));

    [TestCase("preview", ImageSize.Preview)]
    [TestCase(" Fullsize ", ImageSize.Fullsize)]
    [TestCase("ORIGINAL", ImageSize.Original)]
    [TestCase("2", ImageSize.Preview)]
    [TestCase("thumbnail", ImageSize.Preview)]
    [TestCase(null, ImageSize.Preview)]
    public void ImageSizeParser_IsLenient(string? value, ImageSize expected) =>
        Assert.That(ImageSizeParser.Parse(value), Is.EqualTo(expected));
}
