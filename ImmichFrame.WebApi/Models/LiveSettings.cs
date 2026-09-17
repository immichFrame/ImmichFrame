using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Models;

/// <summary>
/// Delegating facade over <see cref="ISettingsProvider.Current"/> so that
/// constructor-injected settings references in singletons never go stale
/// when the settings are updated at runtime.
/// </summary>
public class LiveServerSettings(ISettingsProvider _provider) : IServerSettings
{
    public IEnumerable<IAccountSettings> Accounts => _provider.Current.Accounts;
    public IGeneralSettings GeneralSettings => _provider.Current.GeneralSettings;
    public void Validate() => _provider.Current.Validate();
}

/// <inheritdoc cref="LiveServerSettings"/>
public class LiveGeneralSettings(ISettingsProvider _provider) : IGeneralSettings
{
    private IGeneralSettings Current => _provider.Current.GeneralSettings;

    public int Interval => Current.Interval;
    public double TransitionDuration => Current.TransitionDuration;
    public bool DownloadImages => Current.DownloadImages;
    public int RenewImagesDuration => Current.RenewImagesDuration;
    public bool ShowClock => Current.ShowClock;
    public string? ClockFormat => Current.ClockFormat;
    public string? ClockDateFormat => Current.ClockDateFormat;
    public bool ShowPhotoDate => Current.ShowPhotoDate;
    public bool ShowProgressBar => Current.ShowProgressBar;
    public string? PhotoDateFormat => Current.PhotoDateFormat;
    public bool ShowImageDesc => Current.ShowImageDesc;
    public bool ShowPeopleDesc => Current.ShowPeopleDesc;
    public bool ShowTagsDesc => Current.ShowTagsDesc;
    public bool ShowAlbumName => Current.ShowAlbumName;
    public bool ShowImageLocation => Current.ShowImageLocation;
    public string? ImageLocationFormat => Current.ImageLocationFormat;
    public string? PrimaryColor => Current.PrimaryColor;
    public string? SecondaryColor => Current.SecondaryColor;
    public string Style => Current.Style;
    public string? BaseFontSize => Current.BaseFontSize;
    public bool ShowWeatherDescription => Current.ShowWeatherDescription;
    public string? WeatherIconUrl => Current.WeatherIconUrl;
    public bool ImageZoom => Current.ImageZoom;
    public bool ImagePan => Current.ImagePan;
    public bool ImageFill => Current.ImageFill;
    public bool PlayAudio => Current.PlayAudio;
    public string Layout => Current.Layout;
    public string Language => Current.Language;
    public List<string> Webcalendars => Current.Webcalendars;
    public int RefreshAlbumPeopleInterval => Current.RefreshAlbumPeopleInterval;
    public string? WeatherApiKey => Current.WeatherApiKey;
    public string? WeatherLatLong => Current.WeatherLatLong;
    public string? UnitSystem => Current.UnitSystem;
    public string? Webhook => Current.Webhook;
    public string? AuthenticationSecret => Current.AuthenticationSecret;
    public string? AdminPassword => Current.AdminPassword;

    public void Validate() => Current.Validate();
}
