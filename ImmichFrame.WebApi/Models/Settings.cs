using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Models
{
    public class Settings : ISettings
    {
        public string ImmichServerUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ImageStretch { get; set; } = "Uniform";
        public string Margin { get; set; } = "0,0,0,0";
        public int Interval { get; set; } = 8;
        public double TransitionDuration { get; set; } = 1;
        public bool DownloadImages { get; set; } = false;
        public bool ShowMemories { get; set; } = false;
        public int RenewImagesDuration { get; set; } = 20;
        public List<Guid> Albums { get; set; } = new List<Guid>();
        public List<Guid> ExcludedAlbums { get; set; } = new List<Guid>();
        public List<Guid> People { get; set; } = new List<Guid>();
        public int RefreshAlbumPeopleInterval { get; set; } = 12;
        public string ImmichFrameAlbumName { get; set; } = string.Empty;
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
    }
}
