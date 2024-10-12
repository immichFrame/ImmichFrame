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
        public int RefreshAlbumPeopleInterval { get; set; }
        public string ImmichFrameAlbumName { get; set; }
    }
}