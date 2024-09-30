using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;

namespace ImmichFrame.WebApi.Models
{
    public class ClientSettings : IClientSettings
    {
        public string ImageStretch { get; set; } = "Uniform";
        public string Margin { get; set; } = "0,0,0,0";
        public int Interval { get; set; } = 45;
        public double TransitionDuration { get; set; } = 1;
        public bool DownloadImages { get; set; } = false;
        public bool ShowMemories { get; set; } = false;
        public int RenewImagesDuration { get; set; } = 20;
        public bool ShowClock { get; set; } = true;
        public int ClockFontSize { get; set; } = 48;
        public string? ClockFormat { get; set; } = "h:mm tt";
        public bool ShowPhotoDate { get; set; } = true;
        public int PhotoDateFontSize { get; set; } = 36;
        public string? PhotoDateFormat { get; set; } = "MM/dd/yyyy";
        public bool ShowImageDesc { get; set; } = true;
        public int ImageDescFontSize { get; set; } = 36;
        public bool ShowImageLocation { get; set; } = true;
        public string? ImageLocationFormat { get; set; } = "City,State,Country";
        public int ImageLocationFontSize { get; set; } = 36;
        public string FontColor { get; set; } = "#FFFFFF";
        public string? WeatherApiKey { get; set; } = string.Empty;
        public bool ShowWeatherDescription { get; set; } = true;
        public int WeatherFontSize { get; set; } = 36;
        public string? UnitSystem { get; set; } = "";
        public string? WeatherLatLong { get; set; } = "40.7128,74.0060";
        public string Language { get; set; } = "en";
        public bool UnattendedMode { get; set; } = false;

        public ClientSettings()
        {
            var env = Environment.GetEnvironmentVariables();
            try
            {
                foreach (var key in env.Keys)
                {
                    if (key == null) continue;

                    var propertyInfo = typeof(ClientSettings).GetProperty(key.ToString() ?? string.Empty);

                    if (propertyInfo != null)
                    {
                        this.SetValue(propertyInfo, env[key]?.ToString() ?? string.Empty);
                    }

                }
            }
            catch (Exception ex)
            {
                throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
            }
        }
    }
}
