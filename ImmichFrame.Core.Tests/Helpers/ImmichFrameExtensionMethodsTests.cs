using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Helpers;

[TestFixture]
public class ImmichFrameExtensionMethodsTests
{
    [Test]
    public void SanitizeString_KeepsGuidWithDashes()
    {
        const string guid = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

        Assert.That(guid.SanitizeString(), Is.EqualTo(guid));
    }

    [TestCase("frame\n1", "frame1")]
    [TestCase("frame\r\n1", "frame1")]
    [TestCase("frame\t1", "frame1")]
    [TestCase("\u001b[31mframe\u001b[0m", "31mframe0m")]
    [TestCase("frame\u00001", "frame1")]
    [TestCase("my frame", "myframe")]
    [TestCase("främe\"'<>/\\;", "frme")]
    public void SanitizeString_RemovesDisallowedCharacters(string input, string expected)
    {
        Assert.That(input.SanitizeString(), Is.EqualTo(expected));
    }

    [Test]
    public void SanitizeString_KeepsDashedAndUndashedIdentifiersDistinct()
    {
        Assert.Multiple(() =>
        {
            Assert.That("frame-1".SanitizeString(), Is.EqualTo("frame-1"));
            Assert.That("frame1".SanitizeString(), Is.EqualTo("frame1"));
            Assert.That("frame-1".SanitizeString(), Is.Not.EqualTo("frame1".SanitizeString()));
        });
    }

    [Test]
    public void SanitizeString_KeepsAllowedPunctuation()
    {
        Assert.That("a.b_c-D9".SanitizeString(), Is.EqualTo("a.b_c-D9"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\n")]
    public void SanitizeString_WhitespaceBecomesEmpty(string input)
    {
        Assert.That(input.SanitizeString(), Is.Empty);
    }
}
