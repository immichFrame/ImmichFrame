using ImmichFrame.WebApi.Helpers;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Helpers;

[TestFixture]
public class SecretComparisonTests
{
    [Test]
    public void Matches_EqualSecret_ReturnsTrue()
    {
        Assert.That(SecretComparison.Matches("s3cret-value", "s3cret-value"), Is.True);
    }

    [TestCase("s3cret-valuE")]
    [TestCase("s3cret-value-and-more")]
    [TestCase("s3cret")]
    [TestCase("")]
    [TestCase("S3CRET-VALUE")]
    public void Matches_DifferentSecret_ReturnsFalse(string given)
    {
        Assert.That(SecretComparison.Matches("s3cret-value", given), Is.False);
    }
}
