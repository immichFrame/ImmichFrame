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
        public string? ClockFormat { get; set; } = "hh:mm";
        public bool ShowPhotoDate { get; set; } = true;
        public string? PhotoDateFormat { get; set; } = "MM/dd/yyyy";
        public bool ShowImageDesc { get; set; } = true;
        public bool ShowPeopleDesc { get; set; } = true;
        public bool ShowImageLocation { get; set; } = true;
        public string? ImageLocationFormat { get; set; } = "City,State,Country";
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string Style { get; set; } = "none";
        public string? BaseFontSize { get; set; }
        public bool ShowWeatherDescription { get; set; } = true;
        public bool UnattendedMode { get; set; } = false;
        public bool ImageZoom { get; set; } = true;
        public string Layout { get; set; } = "splitview";
        public string Language { get; set; } = "en";
        public string? Subpage { get; set; }

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
