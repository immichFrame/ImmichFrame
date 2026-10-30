using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Helpers;

[TestFixture]
public class ImmichFrameExtensionMethodsTests
{
    [Test]
    public void SanitizeString_DropsDashesFromAGuid_AsBefore()
    {
        const string guid = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

        Assert.That(guid.SanitizeString(), Is.EqualTo("3f2504e04f8911d39a0c0305e82c3301"));
    }

    [TestCase("frame\n1", "frame1")]
    [TestCase("frame\r\n1", "frame1")]
    [TestCase("frame\t1", "frame1")]
    [TestCase("\u001b[31mframe\u001b[0m", "31mframe0m")]
    [TestCase("frame\u00001", "frame1")]
    [TestCase("frame\u202Eevil", "frameevil")]
    [TestCase("främe\"'<>/\\;", "främe")]
    public void SanitizeString_RemovesDisallowedCharacters(string input, string expected)
    {
        Assert.That(input.SanitizeString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase("Frame Kitchen", "Frame Kitchen")]
    [TestCase("  my frame  ", "my frame")]
    [TestCase("Küche", "Küche")]
    [TestCase("客厅", "客厅")]
    [TestCase("Кухня 2", "Кухня 2")]
    [TestCase("frame-1", "frame1")]
    [TestCase("kitchen_2", "kitchen2")]
    public void SanitizeString_KeepsNamesInAnyScript_AsBefore(string input, string expected)
    {
        Assert.That(input.SanitizeString(), Is.EqualTo(expected));
    }

    [Test]
    public void SanitizeString_KeepsOnlyPeriodsAmongPunctuation()
    {
        Assert.That("a.b_c-D9".SanitizeString(), Is.EqualTo("a.bcD9"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\n")]
    public void SanitizeString_WhitespaceBecomesEmpty(string input)
    {
        Assert.That(input.SanitizeString(), Is.Empty);
    }
}
