namespace ImmichFrame.Core.Interfaces
{
    public interface IBaseSettings
    {
        public string ImmichServerUrl { get; set; }
        public string ApiKey { get; set; }
        public bool ShowMemories { get; set; }
        public List<Guid> Albums { get; set; }
        public List<Guid> ExcludedAlbums { get; set; }
        public List<Guid> People { get; set; }
        public int RefreshAlbumPeopleInterval { get; set; }
        public string ImmichFrameAlbumName { get; set; }
    }
    public interface IFullSettings : IBaseSettings
    {
        public string ImageStretch { get; set; }
        public string Margin { get; set; }
        public int Interval { get; set; }
        public double TransitionDuration { get; set; }
        public bool DownloadImages { get; set; }
        public int RenewImagesDuration { get; set; }
        public bool ShowClock { get; set; }
        public int ClockFontSize { get; set; }
        public string? ClockFormat { get; set; }
        public bool ShowPhotoDate { get; set; }
        public int PhotoDateFontSize { get; set; }
        public string? PhotoDateFormat { get; set; }
        public bool ShowImageDesc { get; set; }
        public int ImageDescFontSize { get; set; }
        public bool ShowImageLocation { get; set; }
        public string? ImageLocationFormat { get; set; }
        public int ImageLocationFontSize { get; set; }
        public string FontColor { get; set; }
        public string? WeatherApiKey { get; set; }
        public bool ShowWeatherDescription { get; set; }
        public int WeatherFontSize { get; set; }
        public string? UnitSystem { get; set; }
        public string? WeatherLatLong { get; set; }
        public string Language { get; set; }
        public bool UnattendedMode { get; set; }
    }
}
