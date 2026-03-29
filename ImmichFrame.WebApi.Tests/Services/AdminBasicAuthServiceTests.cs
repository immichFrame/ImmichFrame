using System.Collections;
using System.Security.Cryptography;
using System.Text;
using ImmichFrame.WebApi.Services;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Services;

[TestFixture]
public class AdminBasicAuthServiceTests
{
    [Test]
    public void LoadUsers_FindsCompleteUserHashPairs()
    {
        IDictionary environment = new Hashtable
        {
            ["IMMICHFRAME_AUTH_BASIC_JOHN_USER"] = "john",
            ["IMMICHFRAME_AUTH_BASIC_JOHN_HASH"] = "{SHA}" + Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes("secret"))),
            ["IMMICHFRAME_AUTH_BASIC_INCOMPLETE_USER"] = "ignored"
        };

        var users = AdminBasicAuthService.LoadUsers(environment);

        Assert.That(users, Has.Count.EqualTo(1));
        Assert.That(users[0].Username, Is.EqualTo("john"));
    }

    [Test]
    public void ValidateCredentials_SupportsApacheMd5AndSha1()
    {
        var aprHash = ApacheMd5Crypt.Hash("secret", "$apr1$xyw6b7Rb$");
        IDictionary environment = new Hashtable
        {
            ["IMMICHFRAME_AUTH_BASIC_SHA_USER"] = "sha-user",
            ["IMMICHFRAME_AUTH_BASIC_SHA_HASH"] =
                "{SHA}" + Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes("sha-password"))),
            ["IMMICHFRAME_AUTH_BASIC_APR_USER"] = "apr-user",
            ["IMMICHFRAME_AUTH_BASIC_APR_HASH"] = aprHash
        };

        var service = new AdminBasicAuthService(environment);

        Assert.Multiple(() =>
        {
            Assert.That(service.HasUsers, Is.True);
            Assert.That(service.ValidateCredentials("sha-user", "sha-password"), Is.True);
            Assert.That(service.ValidateCredentials("apr-user", "secret"), Is.True);
            Assert.That(service.ValidateCredentials("apr-user", "wrong"), Is.False);
        });
    }

    [Test]
    public void LoadUsers_ThrowsForDuplicateTrimmedUsernames()
    {
        IDictionary environment = new Hashtable
        {
            ["IMMICHFRAME_AUTH_BASIC_ONE_USER"] = " michel ",
            ["IMMICHFRAME_AUTH_BASIC_ONE_HASH"] = "{SHA}" + Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes("secret-1"))),
            ["IMMICHFRAME_AUTH_BASIC_TWO_USER"] = "michel",
            ["IMMICHFRAME_AUTH_BASIC_TWO_HASH"] = "{SHA}" + Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes("secret-2")))
        };

        var exception = Assert.Throws<InvalidOperationException>(() => AdminBasicAuthService.LoadUsers(environment));

        Assert.That(exception!.Message, Does.Contain("Duplicate admin username 'michel'"));
        Assert.That(exception.Message, Does.Contain("IMMICHFRAME_AUTH_BASIC_ONE_USER"));
        Assert.That(exception.Message, Does.Contain("IMMICHFRAME_AUTH_BASIC_TWO_USER"));
    }

    [Test]
    public void ValidateCredentials_ReturnsFalseForMalformedBcryptHash()
    {
        IDictionary environment = new Hashtable
        {
            ["IMMICHFRAME_AUTH_BASIC_BAD_USER"] = "bad-user",
            ["IMMICHFRAME_AUTH_BASIC_BAD_HASH"] = "$2bad-hash"
        };

        var service = new AdminBasicAuthService(environment);

        Assert.That(service.ValidateCredentials("bad-user", "secret"), Is.False);
    }
}
