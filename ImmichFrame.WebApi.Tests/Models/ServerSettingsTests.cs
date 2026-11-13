using ImmichFrame.WebApi.Models;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Models;

[TestFixture]
public class ServerSettingsTests
{
    private static ServerAccountSettings Account(string? name) => new()
    {
        Name = name,
        ImmichServerUrl = "http://immich.local",
        ApiKey = "key"
    };

    private static ServerSettings Settings(params ServerAccountSettings[] accounts) => new()
    {
        GeneralSettingsImpl = new GeneralSettings(),
        AccountsImpl = accounts.ToList()
    };

    [TestCase("family")]
    [TestCase("Family_2")]
    [TestCase("living-room")]
    public void Validate_ValidName_IsAccepted(string name)
    {
        var account = Account(name);

        Assert.DoesNotThrow(Settings(account).Validate);
        Assert.That(account.Name, Is.EqualTo(name));
    }

    [TestCase("living room")]
    [TestCase("familie/ferien")]
    [TestCase("äpfel")]
    [TestCase("a&b")]
    public void Validate_InvalidName_Throws(string name)
    {
        Assert.Throws<InvalidOperationException>(Settings(Account(name)).Validate);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_EmptyName_IsNull(string? name)
    {
        var account = Account(name);

        Settings(account).Validate();

        Assert.That(account.Name, Is.Null);
    }

    [Test]
    public void Validate_TrimsName()
    {
        var account = Account("  family ");

        Settings(account).Validate();

        Assert.That(account.Name, Is.EqualTo("family"));
    }

    [Test]
    public void Validate_DuplicateNamesIgnoringCase_Throws()
    {
        var settings = Settings(Account("family"), Account("Family"));

        Assert.Throws<InvalidOperationException>(settings.Validate);
    }

    [Test]
    public void Validate_UnnamedAccounts_AreNotDuplicates()
    {
        var settings = Settings(Account(null), Account(""), Account("family"));

        Assert.DoesNotThrow(settings.Validate);
    }
}
