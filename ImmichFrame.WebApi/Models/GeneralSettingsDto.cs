namespace ImmichFrame.WebApi.Models;

/// <summary>
/// DB-backed general settings. Intentionally excludes AuthenticationSecret (kept in config/env).
/// </summary>
public sealed class GeneralSettingsDto
{
    public bool DownloadImages { get; set; }
    public string Language { get; set; } = "en";
    public string? ImageLocationFormat { get; set; }
    public string? PhotoDateFormat { get; set; }
    public int Interval { get; set; }
    public double TransitionDuration { get; set; }
    public bool ShowClock { get; set; }
    public string? ClockFormat { get; set; }
    public string? ClockDateFormat { get; set; }
    public bool ShowProgressBar { get; set; }
    public bool ShowPhotoDate { get; set; }
    public bool ShowImageDesc { get; set; }
    public bool ShowPeopleDesc { get; set; }
    public bool ShowAlbumName { get; set; }
    public bool ShowImageLocation { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string Style { get; set; } = "none";
    public string? BaseFontSize { get; set; }
    public bool ShowWeatherDescription { get; set; }
    public string? WeatherIconUrl { get; set; }
    public bool ImageZoom { get; set; }
    public bool ImagePan { get; set; }
    public bool ImageFill { get; set; }
    public string Layout { get; set; } = "splitview";
    public int RenewImagesDuration { get; set; }
    public List<string> Webcalendars { get; set; } = new();
    public int RefreshAlbumPeopleInterval { get; set; }
    public string? WeatherApiKey { get; set; }
    public string? UnitSystem { get; set; }
    public string? WeatherLatLong { get; set; }
    public string? Webhook { get; set; }
}


