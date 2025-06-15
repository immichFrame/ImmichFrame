using System.Collections;
using System.Reflection;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Models;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

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
    public void TestLoadConfigV1Json()
    {
        var config = _configLoader.LoadConfigJson<ServerSettingsV1>(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Resources/TestV1.json"));
        VerifyConfig(new ServerSettingsV1Adapter(config), false);
    }

    [Test]
    public void TestLoadConfigEnv()
    {
        var jsonConfig = _configLoader.LoadConfigJson<ServerSettingsV1>(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Resources/TestV1.json"));
        
        var config = _configLoader.LoadConfigFromDictionary<ServerSettingsV1>(ToDictionary(jsonConfig));
        VerifyConfig(new ServerSettingsV1Adapter(config), false);
    }

    [Test]
    public void TestLoadConfigV2Json()
    {
        var config = _configLoader.LoadConfigJson<ServerSettings>(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "Resources/TestV2.json"));
        VerifyConfig(config, true);
    }

    private void VerifyConfig(IServerSettings serverSettings, Boolean usePrefix)
    {
        VerifyProperties(serverSettings.GeneralSettings);

        var idx = 1;
        foreach (var account in serverSettings.Accounts)
        {
            VerifyProperties(account, usePrefix ? "Account" + idx + "." : "");
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
    
    public static IDictionary ToDictionary(object obj, bool ignoreNullValues = false)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var dictionary = new Dictionary<string, object>();
        Type objType = obj.GetType();

        // Get all public instance properties
        PropertyInfo[] properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo prop in properties)
        {
            // Ensure the property has a public getter
            if (prop.CanRead && prop.GetMethod?.IsPublic == true)
            {
                object value = prop.GetValue(obj);

                if (ignoreNullValues && value == null)
                {
                    continue; // Skip if value is null and ignoreNullValues is true
                }

                if (!(value is string) && value is IEnumerable)
                {
                    value = string.Join(",", (value as IEnumerable).Cast<object>().Select(x => x.ToString()));
                }
                else
                {
                    value = value.ToString();
                }
                
                dictionary.Add(prop.Name, value);
            }
        }

        return dictionary;
    }
    
}