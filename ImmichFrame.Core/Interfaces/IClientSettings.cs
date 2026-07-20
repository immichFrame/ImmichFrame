namespace ImmichFrame.Core.Interfaces
{
    public interface IClientSettings
    {
        public int Interval { get; }
        public double TransitionDuration { get; }
        public bool DownloadImages { get; }
        public int RenewImagesDuration { get; }
        public bool ShowClock { get; }
        public string? ClockFormat { get; }
        public string? ClockDateFormat { get; }
        public bool ShowPhotoDate { get; }
        public bool ShowProgressBar { get; }
        public string? PhotoDateFormat { get; }
        public bool ShowImageDesc { get; }
        public bool ShowPeopleDesc { get; }
        public bool ShowTagsDesc { get; }
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
        public bool PlayAudio { get; }
        public string Layout { get; }
        public string Language { get; }
        public bool EventHostEnabled { get; }
        public int EventPollingIntervalSeconds { get; }
        public int EventDefaultTimeoutMs { get; }
    }
}
