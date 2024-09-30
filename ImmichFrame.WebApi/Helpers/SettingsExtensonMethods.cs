using ImmichFrame.WebApi.Models;
using System.Reflection;

namespace ImmichFrame.WebApi.Helpers
{
    public static class SettingsExtensions
    {
        public static void SetValue(this ServerSettings s, PropertyInfo prop, string value)
        {
            SetValue((object)s, prop, value);
        }
        public static void SetValue(this ClientSettings s, PropertyInfo prop, string value)
        {
            SetValue((object)s, prop, value);
        }

        private static void SetValue(object settings, PropertyInfo prop, string value)
        {
            var type = prop.PropertyType;
            if (type == typeof(List<Guid>))
            {
                prop.SetValue(settings, value.ToString()?.Split(',').Select(x => new Guid(x)).ToList());
            }
            else if (type == typeof(string))
            {
                prop.SetValue(settings, value);
            }
            else if (type == typeof(bool))
            {
                prop.SetValue(settings, bool.Parse(value.ToString() ?? "false"));
            }
            else if (type == typeof(int))
            {
                prop.SetValue(settings, Convert.ToInt32(value));
            }
        }
    }
}
