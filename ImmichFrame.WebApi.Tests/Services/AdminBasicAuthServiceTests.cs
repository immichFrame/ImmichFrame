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
        Environment.SetEnvironmentVariable("IMMICHFRAME_AUTH_BASIC_SHA_USER", "sha-user");
        Environment.SetEnvironmentVariable("IMMICHFRAME_AUTH_BASIC_SHA_HASH", "{SHA}" + Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes("sha-password"))));
        Environment.SetEnvironmentVariable("IMMICHFRAME_AUTH_BASIC_APR_USER", "apr-user");
        Environment.SetEnvironmentVariable("IMMICHFRAME_AUTH_BASIC_APR_HASH", aprHash);

        try
        {
            var service = new AdminBasicAuthService();

            Assert.Multiple(() =>
            {
                Assert.That(service.HasUsers, Is.True);
                Assert.That(service.ValidateCredentials("sha-user", "sha-password"), Is.True);
                Assert.That(service.ValidateCredentials("apr-user", "secret"), Is.True);
                Assert.That(service.ValidateCredentials("apr-user", "wrong"), Is.False);
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable("IMMICHFRAME_AUTH_BASIC_SHA_USER", null);
            Environment.SetEnvironmentVariable("IMMICHFRAME_AUTH_BASIC_SHA_HASH", null);
            Environment.SetEnvironmentVariable("IMMICHFRAME_AUTH_BASIC_APR_USER", null);
            Environment.SetEnvironmentVariable("IMMICHFRAME_AUTH_BASIC_APR_HASH", null);
        }
    }
}
