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
    public string BaseUrl { get; set; } = "/";
}

/// <summary>
/// Adapter to present a SettingsV1 object as an IServerSettings
/// </summary>
/// <param name="Settings">the V1 settings object to wrap</param>
public class ServerSettingsV1Adapter(ServerSettingsV1 Settings) : IServerSettings
{
    public ServerSettingsV1 Settings { get; } = Settings;
    public IEnumerable<IAccountSettings> Accounts => new List<AccountSettingsV1Adapter> { new(Settings) };
    public IGeneralSettings GeneralSettings => new GeneralSettingsV1Adapter(Settings);

    public void Validate()
    {
        GeneralSettings.Validate();
        foreach (var account in Accounts)
        {
            account.ValidateAndInitialize();
        }
    }

    class AccountSettingsV1Adapter(ServerSettingsV1 Settings) : IAccountSettings
    {
        public string ImmichServerUrl => Settings.ImmichServerUrl;
        public string ApiKey => Settings.ApiKey;
        public string? ApiKeyFile => null;  // V1 settings didn't support paths to api keys.
        public bool ShowMemories => Settings.ShowMemories;
        public bool ShowFavorites => Settings.ShowFavorites;
        public bool ShowArchived => Settings.ShowArchived;
        public int? ImagesFromDays => Settings.ImagesFromDays;
        public DateTime? ImagesFromDate => Settings.ImagesFromDate;
        public DateTime? ImagesUntilDate => Settings.ImagesUntilDate;
        public List<Guid> Albums => Settings.Albums;
        public List<Guid> ExcludedAlbums => Settings.ExcludedAlbums;
        public List<Guid> People => Settings.People;
        public int? Rating => Settings.Rating;

        public void ValidateAndInitialize() { }
    }

    class GeneralSettingsV1Adapter(ServerSettingsV1 Settings) : IGeneralSettings
    {
        public List<string> Webcalendars => Settings.Webcalendars;
        public int RefreshAlbumPeopleInterval => Settings.RefreshAlbumPeopleInterval;
        public string? WeatherApiKey => Settings.WeatherApiKey;
        public string? WeatherLatLong => Settings.WeatherLatLong;
        public string? UnitSystem => Settings.UnitSystem;
        public string? Webhook => Settings.Webhook;
        public string? AuthenticationSecret => Settings.AuthenticationSecret;
        public int Interval => Settings.Interval;
        public double TransitionDuration => Settings.TransitionDuration;
        public bool DownloadImages => Settings.DownloadImages;
        public int RenewImagesDuration => Settings.RenewImagesDuration;
        public bool ShowClock => Settings.ShowClock;
        public string? ClockFormat => Settings.ClockFormat;
        public string? ClockDateFormat => Settings.ClockDateFormat;
        public bool ShowProgressBar => Settings.ShowProgressBar;
        public bool ShowPhotoDate => Settings.ShowPhotoDate;
        public string? PhotoDateFormat => Settings.PhotoDateFormat;
        public bool ShowImageDesc => Settings.ShowImageDesc;
        public bool ShowPeopleDesc => Settings.ShowPeopleDesc;
        public bool ShowAlbumName => Settings.ShowAlbumName;
        public bool ShowImageLocation => Settings.ShowImageLocation;
        public string? ImageLocationFormat => Settings.ImageLocationFormat;
        public string? PrimaryColor => Settings.PrimaryColor;
        public string? SecondaryColor => Settings.SecondaryColor;
        public string Style => Settings.Style;
        public string? BaseFontSize => Settings.BaseFontSize;
        public bool ShowWeatherDescription => Settings.ShowWeatherDescription;
        public string? WeatherIconUrl => Settings.WeatherIconUrl;
        public bool ImageZoom => Settings.ImageZoom;
        public bool ImagePan => Settings.ImagePan;
        public bool ImageFill => Settings.ImageFill;
        public string Layout => Settings.Layout;
        public string Language => Settings.Language;
        public string BaseUrl => Settings.BaseUrl;

        public void Validate() { }
    }
}
