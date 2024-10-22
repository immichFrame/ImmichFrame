namespace ImmichFrame.Core.Interfaces
{
    public interface IFullSettings : IServerSettings, IClientSettings
    { }

    public interface IServerSettings
    {
        public string ImmichServerUrl { get; set; }
        public string ApiKey { get; set; }
        public bool ShowMemories { get; set; }
        public List<Guid> Albums { get; set; }
        public List<Guid> ExcludedAlbums { get; set; }
        public List<Guid> People { get; set; }
        public List<string> Webcalendars { get; set; }
        public int RefreshAlbumPeopleInterval { get; set; }
        public string ImmichFrameAlbumName { get; set; }
        public string? WeatherApiKey { get; set; }
        public string? WeatherLatLong { get; set; }
        public string? UnitSystem { get; set; }
        public string Language { get; set; }
    }
}