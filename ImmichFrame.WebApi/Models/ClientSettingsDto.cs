using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Models;

public class ClientSettingsDto(IClientSettings settings) : IClientSettings
{
    public int Interval => settings.Interval;
    public double TransitionDuration => settings.TransitionDuration;
    public bool DownloadImages => settings.DownloadImages;
    public int RenewImagesDuration => settings.RenewImagesDuration;
    public bool ShowClock => settings.ShowClock;
    public string? ClockFormat => settings.ClockFormat;
    public string? ClockDateFormat => settings.ClockDateFormat;
    public bool ShowPhotoDate => settings.ShowPhotoDate;
    public bool ShowProgressBar => settings.ShowProgressBar;
    public string? PhotoDateFormat => settings.PhotoDateFormat;
    public bool ShowImageDesc => settings.ShowImageDesc;
    public bool ShowPeopleDesc => settings.ShowPeopleDesc;
    public bool ShowTagsDesc => settings.ShowTagsDesc;
    public bool ShowAlbumName => settings.ShowAlbumName;
    public bool ShowImageLocation => settings.ShowImageLocation;
    public string? ImageLocationFormat => settings.ImageLocationFormat;
    public string? PrimaryColor => settings.PrimaryColor;
    public string? SecondaryColor => settings.SecondaryColor;
    public string Style => settings.Style;
    public string? BaseFontSize => settings.BaseFontSize;
    public bool ShowWeatherDescription => settings.ShowWeatherDescription;
    public string? WeatherIconUrl => settings.WeatherIconUrl;
    public bool ImageZoom => settings.ImageZoom;
    public bool ImagePan => settings.ImagePan;
    public bool ImageFill => settings.ImageFill;
    public bool PlayAudio => settings.PlayAudio;
    public string Layout => settings.Layout;
    public string Language => settings.Language;
    public bool EventHostEnabled => settings.EventHostEnabled;
    public int EventPollingIntervalSeconds => settings.EventPollingIntervalSeconds;
    public int EventDefaultTimeoutMs => settings.EventDefaultTimeoutMs;
}
