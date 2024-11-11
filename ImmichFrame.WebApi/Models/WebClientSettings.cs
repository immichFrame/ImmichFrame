using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;

namespace ImmichFrame.WebApi.Models
{
    public class WebClientSettings : IWebClientSettings
    {
        public string ImageStretch { get; set; } = "Uniform";
        public string Margin { get; set; } = "0,0,0,0";
        public int Interval { get; set; } = 45;
        public double TransitionDuration { get; set; } = 1;
        public bool DownloadImages { get; set; } = false;
        public int RenewImagesDuration { get; set; } = 20;
        public bool ShowClock { get; set; } = true;
        public int ClockFontSize { get; set; } = 48;
        public string? ClockFormat { get; set; } = "hh:mm";
        public bool ShowPhotoDate { get; set; } = true;
        public int PhotoDateFontSize { get; set; } = 36;
        public string? PhotoDateFormat { get; set; } = "MM/dd/yyyy";
        public bool ShowImageDesc { get; set; } = true;
        public int ImageDescFontSize { get; set; } = 36;
        public bool ShowImageLocation { get; set; } = true;
        public string? ImageLocationFormat { get; set; } = "City,State,Country";
        public int ImageLocationFontSize { get; set; } = 36;
        public string FontColor { get; set; }
        public bool ShowWeatherDescription { get; set; } = true;
        public int WeatherFontSize { get; set; } = 36;
        public bool UnattendedMode { get; set; } = false;
        public bool ImageZoom { get; set; } = true;

        public WebClientSettings()
        {
            var env = Environment.GetEnvironmentVariables();
            try
            {
                foreach (var key in env.Keys)
                {
                    if (key == null) continue;

                    var propertyInfo = typeof(WebClientSettings).GetProperty(key.ToString() ?? string.Empty);

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
