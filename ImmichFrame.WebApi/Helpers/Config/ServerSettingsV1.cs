using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Helpers;

/// <summary>
/// Original 'flat' Settings definition, which contains settings related to all components
/// </summary>
public class ServerSettingsV1 : IConfigSettable
{
    public string ImmichServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool ShowMemories { get; set; } = false;
    public bool ShowFavorites { get; set; } = false;
    public bool ShowArchived { get; set; } = false;
    public bool DownloadImages { get; set; } = false;
    public int RenewImagesDuration { get; set; } = 30;
    public int? ImagesFromDays { get; set; }
    public DateTime? ImagesFromDate { get; set; }
    public DateTime? ImagesUntilDate { get; set; }
    public List<Guid> Albums { get; set; } = new List<Guid>();
    public List<Guid> ExcludedAlbums { get; set; } = new List<Guid>();
    public List<Guid> People { get; set; } = new List<Guid>();
    public int? Rating { get; set; }
    public List<string> Webcalendars { get; set; } = new List<string>();
    public int RefreshAlbumPeopleInterval { get; set; } = 12;
    public string? WeatherApiKey { get; set; } = string.Empty;
    public string? UnitSystem { get; set; } = "imperial";
    public string? WeatherLatLong { get; set; } = "40.7128,74.0060";
    public string Language { get; set; } = "en";
    public string? Webhook { get; set; }
    public string? AuthenticationSecret { get; set; }
    public int Interval { get; set; } = 45;
    public double TransitionDuration { get; set; } = 1;
    public bool ShowClock { get; set; } = true;
    public string? ClockFormat { get; set; } = "hh:mm";
    public string? ClockDateFormat { get; set; } = "eee, MMM d";
    public bool ShowProgressBar { get; set; } = true;
    public bool ShowPhotoDate { get; set; } = true;
    public string? PhotoDateFormat { get; set; } = "MM/dd/yyyy";
    public bool ShowImageDesc { get; set; } = true;
    public bool ShowPeopleDesc { get; set; } = true;
    public bool ShowAlbumName { get; set; } = true;
    public bool ShowImageLocation { get; set; } = true;
    public string? ImageLocationFormat { get; set; } = "City,State,Country";
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string Style { get; set; } = "none";
    public string? BaseFontSize { get; set; }
    public bool ShowWeatherDescription { get; set; } = true;
    public string? WeatherIconUrl { get; set; } = "https://openweathermap.org/img/wn/{IconId}.png";
    public bool ImageZoom { get; set; } = true;
    public bool ImagePan { get; set; } = false;
    public bool ImageFill { get; set; } = false;
    public string Layout { get; set; } = "splitview";
    public int ChronologicalImagesCount { get; set; } = 3;
}

/// <summary>
/// Adapter to present a SettingsV1 object as an IServerSettings
/// </summary>
/// <param name="_delegate">the V1 settings object to wrap</param>
public class ServerSettingsV1Adapter(ServerSettingsV1 _delegate) : IServerSettings
{
    public IEnumerable<IAccountSettings> Accounts => new List<AccountSettingsV1Adapter> { new(_delegate) };
    public IGeneralSettings GeneralSettings => new GeneralSettingsV1Adapter(_delegate);


    class AccountSettingsV1Adapter(ServerSettingsV1 _delegate) : IAccountSettings
    {
        public string ImmichServerUrl => _delegate.ImmichServerUrl;
        public string ApiKey => _delegate.ApiKey;
        public bool ShowMemories => _delegate.ShowMemories;
        public bool ShowFavorites => _delegate.ShowFavorites;
        public bool ShowArchived => _delegate.ShowArchived;
        public int? ImagesFromDays => _delegate.ImagesFromDays;
        public DateTime? ImagesFromDate => _delegate.ImagesFromDate;
        public DateTime? ImagesUntilDate => _delegate.ImagesUntilDate;
        public List<Guid> Albums => _delegate.Albums;
        public List<Guid> ExcludedAlbums => _delegate.ExcludedAlbums;
        public List<Guid> People => _delegate.People;
        public int? Rating => _delegate.Rating;
    }

    class GeneralSettingsV1Adapter(ServerSettingsV1 _delegate) : IGeneralSettings
    {
        public List<string> Webcalendars => _delegate.Webcalendars;
        public int RefreshAlbumPeopleInterval => _delegate.RefreshAlbumPeopleInterval;
        public string? WeatherApiKey => _delegate.WeatherApiKey;
        public string? WeatherLatLong => _delegate.WeatherLatLong;
        public string? UnitSystem => _delegate.UnitSystem;
        public string? Webhook => _delegate.Webhook;
        public string? AuthenticationSecret => _delegate.AuthenticationSecret;
        public int Interval => _delegate.Interval;
        public double TransitionDuration => _delegate.TransitionDuration;
        public bool DownloadImages => _delegate.DownloadImages;
        public int RenewImagesDuration => _delegate.RenewImagesDuration;
        public bool ShowClock => _delegate.ShowClock;
        public string? ClockFormat => _delegate.ClockFormat;
        public string? ClockDateFormat => _delegate.ClockDateFormat;
        public bool ShowProgressBar => _delegate.ShowProgressBar;
        public bool ShowPhotoDate => _delegate.ShowPhotoDate;
        public string? PhotoDateFormat => _delegate.PhotoDateFormat;
        public bool ShowImageDesc => _delegate.ShowImageDesc;
        public bool ShowPeopleDesc => _delegate.ShowPeopleDesc;
        public bool ShowAlbumName => _delegate.ShowAlbumName;
        public bool ShowImageLocation => _delegate.ShowImageLocation;
        public string? ImageLocationFormat => _delegate.ImageLocationFormat;
        public string? PrimaryColor => _delegate.PrimaryColor;
        public string? SecondaryColor => _delegate.SecondaryColor;
        public string Style => _delegate.Style;
        public string? BaseFontSize => _delegate.BaseFontSize;
        public bool ShowWeatherDescription => _delegate.ShowWeatherDescription;
        public string? WeatherIconUrl => _delegate.WeatherIconUrl;
        public bool ImageZoom => _delegate.ImageZoom;
        public bool ImagePan => _delegate.ImagePan;
        public bool ImageFill => _delegate.ImageFill;
        public string Layout => _delegate.Layout;
        public string Language => _delegate.Language;
        public int ChronologicalImagesCount => _delegate.ChronologicalImagesCount;
    }
}