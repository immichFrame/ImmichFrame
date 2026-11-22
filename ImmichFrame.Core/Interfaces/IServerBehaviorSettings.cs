namespace ImmichFrame.Core.Interfaces
{
    public interface IServerBehaviorSettings
    {
        public List<string> Webcalendars { get; }
        public int RefreshAlbumPeopleInterval { get; }
        // preview | fullsize | original (Models.ImageSize). Server-side only: the client just gets the image.
        public string ImageSize { get; }
        public string? WeatherApiKey { get; }
        public string? WeatherLatLong { get; }
        public string? UnitSystem { get; }
        public string? Webhook { get; }
        public string? AuthenticationSecret { get; }
        public string? AdminPassword { get; }
    }
}
