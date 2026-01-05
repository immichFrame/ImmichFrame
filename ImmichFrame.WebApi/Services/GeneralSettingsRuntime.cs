using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Services;

/// <summary>
/// Process-wide, live view of general settings.
/// Backed by SQLite (for all settings except AuthenticationSecret).
/// </summary>
public sealed class GeneralSettingsRuntime : IGeneralSettings
{
    private readonly string? _authenticationSecret;
    private volatile GeneralSettingsDto _current;
    private long _currentVersion;

    public GeneralSettingsRuntime(IServerSettings baseSettings)
    {
        _authenticationSecret = baseSettings.GeneralSettings.AuthenticationSecret;
        _current = new GeneralSettingsDto
        {
            DownloadImages = baseSettings.GeneralSettings.DownloadImages,
            Language = baseSettings.GeneralSettings.Language,
            ImageLocationFormat = baseSettings.GeneralSettings.ImageLocationFormat,
            PhotoDateFormat = baseSettings.GeneralSettings.PhotoDateFormat,
            Interval = baseSettings.GeneralSettings.Interval,
            TransitionDuration = baseSettings.GeneralSettings.TransitionDuration,
            ShowClock = baseSettings.GeneralSettings.ShowClock,
            ClockFormat = baseSettings.GeneralSettings.ClockFormat,
            ClockDateFormat = baseSettings.GeneralSettings.ClockDateFormat,
            ShowProgressBar = baseSettings.GeneralSettings.ShowProgressBar,
            ShowPhotoDate = baseSettings.GeneralSettings.ShowPhotoDate,
            ShowImageDesc = baseSettings.GeneralSettings.ShowImageDesc,
            ShowPeopleDesc = baseSettings.GeneralSettings.ShowPeopleDesc,
            ShowAlbumName = baseSettings.GeneralSettings.ShowAlbumName,
            ShowImageLocation = baseSettings.GeneralSettings.ShowImageLocation,
            PrimaryColor = baseSettings.GeneralSettings.PrimaryColor,
            SecondaryColor = baseSettings.GeneralSettings.SecondaryColor,
            Style = baseSettings.GeneralSettings.Style,
            BaseFontSize = baseSettings.GeneralSettings.BaseFontSize,
            ShowWeatherDescription = baseSettings.GeneralSettings.ShowWeatherDescription,
            WeatherIconUrl = baseSettings.GeneralSettings.WeatherIconUrl,
            ImageZoom = baseSettings.GeneralSettings.ImageZoom,
            ImagePan = baseSettings.GeneralSettings.ImagePan,
            ImageFill = baseSettings.GeneralSettings.ImageFill,
            Layout = baseSettings.GeneralSettings.Layout,
            RenewImagesDuration = baseSettings.GeneralSettings.RenewImagesDuration,
            Webcalendars = baseSettings.GeneralSettings.Webcalendars.ToList(),
            RefreshAlbumPeopleInterval = baseSettings.GeneralSettings.RefreshAlbumPeopleInterval,
            WeatherApiKey = baseSettings.GeneralSettings.WeatherApiKey,
            UnitSystem = baseSettings.GeneralSettings.UnitSystem,
            WeatherLatLong = baseSettings.GeneralSettings.WeatherLatLong,
            Webhook = baseSettings.GeneralSettings.Webhook
        };
        _currentVersion = 0;
    }

    public long CurrentVersion => Volatile.Read(ref _currentVersion);

    public void ApplyFromDb(GeneralSettingsDto dto, long version)
    {
        _current = dto;
        Volatile.Write(ref _currentVersion, version);
    }

    public List<string> Webcalendars => _current.Webcalendars;
    public int RefreshAlbumPeopleInterval => _current.RefreshAlbumPeopleInterval;
    public string? WeatherApiKey => _current.WeatherApiKey;
    public string? WeatherLatLong => _current.WeatherLatLong;
    public string? UnitSystem => _current.UnitSystem;
    public string? Webhook => _current.Webhook;
    public string? AuthenticationSecret => _authenticationSecret;
    public int Interval => _current.Interval;
    public double TransitionDuration => _current.TransitionDuration;
    public bool DownloadImages => _current.DownloadImages;
    public int RenewImagesDuration => _current.RenewImagesDuration;
    public bool ShowClock => _current.ShowClock;
    public string? ClockFormat => _current.ClockFormat;
    public string? ClockDateFormat => _current.ClockDateFormat;
    public bool ShowProgressBar => _current.ShowProgressBar;
    public bool ShowPhotoDate => _current.ShowPhotoDate;
    public string? PhotoDateFormat => _current.PhotoDateFormat;
    public bool ShowImageDesc => _current.ShowImageDesc;
    public bool ShowPeopleDesc => _current.ShowPeopleDesc;
    public bool ShowAlbumName => _current.ShowAlbumName;
    public bool ShowImageLocation => _current.ShowImageLocation;
    public string? ImageLocationFormat => _current.ImageLocationFormat;
    public string? PrimaryColor => _current.PrimaryColor;
    public string? SecondaryColor => _current.SecondaryColor;
    public string Style => _current.Style;
    public string? BaseFontSize => _current.BaseFontSize;
    public bool ShowWeatherDescription => _current.ShowWeatherDescription;
    public string? WeatherIconUrl => _current.WeatherIconUrl;
    public bool ImageZoom => _current.ImageZoom;
    public bool ImagePan => _current.ImagePan;
    public bool ImageFill => _current.ImageFill;
    public string Layout => _current.Layout;
    public string Language => _current.Language;

    public void Validate() { }
}


