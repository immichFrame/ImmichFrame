namespace ImmichFrame.Core.Interfaces
{
    public interface IWebClientSettings : IClientSettings
    {

    }

    public interface IClientSettings
    {
        public string ImageStretch { get; set; }
        public string Margin { get; set; }
        public int Interval { get; set; }
        public double TransitionDuration { get; set; }
        public bool DownloadImages { get; set; }
        public int RenewImagesDuration { get; set; }
        public bool ShowClock { get; set; }
        public string? ClockFormat { get; set; }
        public bool ShowPhotoDate { get; set; }
        public string? PhotoDateFormat { get; set; }
        public bool ShowImageDesc { get; set; }
        public bool ShowPeopleDesc { get; set; }
        public bool ShowImageLocation { get; set; }
        public string? ImageLocationFormat { get; set; }
        public string? FontColor { get; set; }
        public bool ShowWeatherDescription { get; set; }
        public bool UnattendedMode { get; set; }
        public bool ImageZoom { get; set; }
        public string Layout { get; set; }
    }
}