using System.Collections;
using System.Reflection;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Models;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using AwesomeAssertions;

namespace ImmichFrame.WebApi.Tests.Helpers.Config;

[TestFixture]
public class ConfigLoaderTest
{
    private string _testDataPath;
    private ConfigLoader _configLoader;

    [SetUp]
    public void Setup()
    {
        _testDataPath = TestContext.CurrentContext.TestDirectory;
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _configLoader = new ConfigLoader(loggerFactory.CreateLogger<ConfigLoader>());
    }

    [Test]
    public void TestLoadConfigV2Json()
    {
        var config = _configLoader.LoadConfigJson<ServerSettings>(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Resources/TestV2.json"));
        VerifyConfig(config);
    }
    
    [Test]
    public void TestLoadConfigV2Json_NoGeneral()
    {
        var config = _configLoader.LoadConfigJson<ServerSettings>(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Resources/TestV2_NoGeneral.json"));
        
        Assert.That(config.GeneralSettings, Is.Not.Null);
        config.GeneralSettings.Should().BeEquivalentTo(new GeneralSettings());
    }

    [Test]
    public void TestLoadConfigV2Yaml()
    {
        var config = _configLoader.LoadConfigYaml<ServerSettings>(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Resources/TestV2.yml"));
        VerifyConfig(config);
    }

    private void VerifyConfig(IServerSettings serverSettings)
    {
        VerifyProperties(serverSettings.GeneralSettings);
        VerifyAccounts(serverSettings.Accounts);
    }

    private void VerifyAccounts(IEnumerable<IAccountSettings> accounts)
    {
        var idx = 1;
        foreach (var account in accounts)
        {
            VerifyProperties(account, "Account" + idx + ".");
            idx++;
        }
    }

    private void VerifyProperties(object o, string? prefix = "")
    {
        foreach (var prop in o.GetType().GetProperties())
        {
            var type = prop.PropertyType;
            object? value = prop.GetValue(o);

            //if it's a list, check the first element
            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
            {
                type = type.GetGenericArguments()[0];
                value = (value as IEnumerable).Cast<object>().FirstOrDefault();
            }

            //if it's nullable, unwrap
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            switch (type)
            {
                case var t when t == typeof(string):
                    Assert.That(value, Is.EqualTo(prefix + prop.Name + "_TEST"), prop.Name);
                    break;
                case var t when t == typeof(Boolean):
                    Assert.That(value, Is.EqualTo(true), prop.Name);
                    break;
                case var t when t == typeof(int):
                    Assert.That(value, Is.EqualTo(7), prop.Name);
                    break;
                case var t when t == typeof(double):
                    Assert.That(value, Is.EqualTo(7.7d), prop.Name);
                    break;
                case var t when t == typeof(Guid):
                    Assert.That(value, Is.Not.EqualTo(Guid.Empty), prop.Name);
                    break;
                case var t when t == typeof(DateTime):
                    Assert.That(value, Is.Not.EqualTo(DateTime.MinValue), prop.Name);
                    break;
                default:
                    throw new NotImplementedException($"Not implemented for {prop.Name} as type ${type}");
            }
        }
    }

}
