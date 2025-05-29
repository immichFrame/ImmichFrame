using System.Reflection;
using System.Text.RegularExpressions;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;

namespace ImmichFrame.WebApi.Models
{
    public class ServerSettings : IServerSettings
    {
        public class ServerFrameSettings : IImmichFrameSettings
        {
            public bool DownloadImages { get; set; } = false;
            public int RenewImagesDuration { get; set; } = 30;
            public List<string> Webcalendars { get; set; } = new List<string>();
            public int RefreshAlbumPeopleInterval { get; set; } = 12;
            public string? WeatherApiKey { get; set; } = string.Empty;
            public string? UnitSystem { get; set; } = "imperial";
            public string? WeatherLatLong { get; set; } = "40.7128,74.0060";
            public string Language { get; set; } = "en";
            public string? Webhook { get; set; }
            public string? AuthenticationSecret { get; set; }
        }

        public class ServerAccountSettings : IImmichAccountSettings
        {
            public string ImmichServerUrl { get; set; } = string.Empty;
            public string ApiKey { get; set; } = string.Empty;
            public bool ShowMemories { get; set; } = false;
            public bool ShowFavorites { get; set; } = false;
            public bool ShowArchived { get; set; } = false;

            public int? ImagesFromDays { get; set; }
            public DateTime? ImagesFromDate { get; set; }
            public DateTime? ImagesUntilDate { get; set; }
            public List<Guid> Albums { get; set; } = new List<Guid>();
            public List<Guid> ExcludedAlbums { get; set; } = new List<Guid>();
            public List<Guid> People { get; set; } = new List<Guid>();
            public int? Rating { get; set; }

            public string ImmichFrameAlbumName { get; set; } = string.Empty;
        }

        public IImmichFrameSettings ImmichFrameSettings { get; set; } = new ServerFrameSettings();
        public IList<IImmichAccountSettings> Accounts { get; set; } = new List<IImmichAccountSettings>();


        private Regex _accountSettingRegex = new(@"^Account(\d)\.(\w+)$");

        public ServerSettings()
        {
            var env = Environment.GetEnvironmentVariables();

            try
            {
                foreach (var key in env.Keys)
                {
                    var sKey = key?.ToString();

                    if (key == null || sKey == null) continue;

                    var matches = _accountSettingRegex.Matches(sKey);

                    if (matches.Any())
                    {
                        var idx = Int32.Parse(matches[0].Groups[1].Value);
                        var strKey = matches[0].Groups[2].Value;
                        var propertyInfo = typeof(ServerAccountSettings).GetProperty(strKey);

                        if (propertyInfo != null)
                            AddAccountProperty(idx, sKey, propertyInfo, env);
                    }
                    else
                    {
                        var propertyInfo = typeof(ServerFrameSettings).GetProperty(sKey);

                        if (propertyInfo != null)
                        {
                            ImmichFrameSettings.SetValue(propertyInfo, env[key]?.ToString() ?? string.Empty);
                        }
                        else
                        {
                            propertyInfo = typeof(ServerAccountSettings).GetProperty(sKey);

                            if (propertyInfo != null)
                            {
                                //Allow raw Account settings for first account & backwards compatibility
                                AddAccountProperty(1, sKey, propertyInfo, env);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = $"Problem with parsing the settings: {ex.Message}";
                throw new SettingsNotValidException(msg, ex);
            }
        }

        private void AddAccountProperty(Int32 idx, string key, PropertyInfo propertyInfo, System.Collections.IDictionary env)
        {
            while (Accounts.Count < idx)
            {
                Accounts.Add(new ServerAccountSettings());
            }

            Accounts[idx - 1].SetValue(propertyInfo, env[key]?.ToString() ?? string.Empty);
        }
    }
}