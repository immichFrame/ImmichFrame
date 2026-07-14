namespace ImmichFrame.Core.Interfaces
{
    public interface IServerBehaviorSettings
    {
        public List<string> Webcalendars { get; }
        public int RefreshAlbumPeopleInterval { get; }
        public string? WeatherApiKey { get; }
        public string? WeatherLatLong { get; }
        public string? UnitSystem { get; }
        public string? Webhook { get; }
        public string? AuthenticationSecret { get; }
    }
}
