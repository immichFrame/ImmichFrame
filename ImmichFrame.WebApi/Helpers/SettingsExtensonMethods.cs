using ImmichFrame.WebApi.Models;
using System.Reflection;

namespace ImmichFrame.WebApi.Helpers
{
    public interface IConfigSettable
    {
        // marker interface denoting settable config class
    }

    public static class SettingsExtensions
    {
        public static void SetValue(this IConfigSettable settings, PropertyInfo prop, string value)
        {
            var type = prop.PropertyType;
            if (type == typeof(List<Guid>))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    prop.SetValue(settings, new List<Guid>());
                    return;
                }

                prop.SetValue(settings, value.Split(',').Select(x => new Guid(x.Trim())).ToList());
            }
            else if (type == typeof(List<string>))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    prop.SetValue(settings, new List<string>());
                    return;
                }

                prop.SetValue(settings, value.Split(',').Select(x => x.Trim()).ToList());
            }
            else if (type == typeof(string))
            {
                prop.SetValue(settings, value);
            }
            else if (type == typeof(bool))
            {
                prop.SetValue(settings, bool.Parse(value));
            }
            else if (type == typeof(int) || type == typeof(int?))
            {
                if (string.IsNullOrWhiteSpace(value)) return;

                prop.SetValue(settings, Convert.ToInt32(value));
            }
            else if (type == typeof(double))
            {
                prop.SetValue(settings, Convert.ToDouble(value));
            }
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                if (string.IsNullOrWhiteSpace(value)) return;

                prop.SetValue(settings, Convert.ToDateTime(value));
            }
            else
            {
                throw new ArgumentException($"{prop.Name} could not be parsed: {type.Name} is not supported in {nameof(SettingsExtensions)}.{nameof(SetValue)}");
            }
        }
    }
}
