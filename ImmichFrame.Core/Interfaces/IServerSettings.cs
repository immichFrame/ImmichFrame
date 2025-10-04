namespace ImmichFrame.Core.Interfaces
{
    public interface IServerSettings
    {
        public IEnumerable<IAccountSettings> Accounts { get; }
        public IGeneralSettings GeneralSettings { get; }
    }

    public interface IAccountSettings
    {
        public string ImmichServerUrl { get; }
        public string ApiKey { get; }
        public bool ShowMemories { get; }
        public bool ShowFavorites { get; }
        public bool ShowArchived { get; }
        public int? ImagesFromDays { get; }
        public DateTime? ImagesFromDate { get; }
        public DateTime? ImagesUntilDate { get; }
        public List<Guid> Albums { get; }
        public List<Guid> ExcludedAlbums { get; }
        public List<Guid> People { get; }
        public int? Rating { get; }
    }

    public interface IGeneralSettings
    {
        public List<string> Webcalendars { get; }
        public int RefreshAlbumPeopleInterval { get; }
        public string? WeatherApiKey { get; }
        public string? WeatherLatLong { get; }
        public string? UnitSystem { get; }
        public string? Webhook { get; }
        public string? AuthenticationSecret { get; }
        public int Interval { get; }
        public double TransitionDuration { get; }
        public bool DownloadImages { get; }
        public int RenewImagesDuration { get; }
        public bool ShowClock { get; }
        public string? ClockFormat { get; }
        public string? ClockDateFormat { get; }
        public bool ShowProgressBar { get; }
        public bool ShowPhotoDate { get; }
        public string? PhotoDateFormat { get; }
        public bool ShowImageDesc { get; }
        public bool ShowPeopleDesc { get; }
        public bool ShowAlbumName { get; }
        public bool ShowImageLocation { get; }
        public string? ImageLocationFormat { get; }
        public string? PrimaryColor { get; }
        public string? SecondaryColor { get; }
        public string Style { get; }
        public string? BaseFontSize { get; }
        public bool ShowWeatherDescription { get; }
        public string? WeatherIconUrl { get; }
        public bool ImageZoom { get; }
        public bool ImagePan { get; }
        public bool ImageFill { get; }
        public string Layout { get; }
        public string Language { get; }
        public bool ShowChronologicalImages { get; }
        public int ChronologicalImagesCount { get; }
    }
}