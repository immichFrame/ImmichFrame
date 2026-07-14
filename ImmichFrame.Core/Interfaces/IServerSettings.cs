namespace ImmichFrame.Core.Interfaces
{
    public interface IServerSettings
    {
        public IEnumerable<IAccountSettings> Accounts { get; }
        public IGeneralSettings GeneralSettings { get; }

        public void Validate();
    }

    public interface IAccountSettings
    {
        public string ImmichServerUrl { get; }
        public string ApiKey { get; }
        public string? ApiKeyFile { get; }
        public bool ShowMemories { get; }
        public bool ShowFavorites { get; }
        public bool ShowArchived { get; }
        public bool ShowVideos { get; }
        public int? ImagesFromDays { get; }
        public DateTime? ImagesFromDate { get; }
        public DateTime? ImagesUntilDate { get; }
        public List<Guid> Albums { get; }
        public List<Guid> ExcludedAlbums { get; }
        public List<Guid> People { get; }
        public List<string> Tags { get; }
        public int? Rating { get; }

        public void ValidateAndInitialize();
    }

    public interface IGeneralSettings : IClientSettings, IServerBehaviorSettings
    {
        public void Validate();
    }
}
