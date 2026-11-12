using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Logic;

public class AlbumOverrideAccountSettings(IAccountSettings inner, IEnumerable<Guid> albums) : IAccountSettings
{
    public string? Name => inner.Name;
    public string ImmichServerUrl => inner.ImmichServerUrl;
    public string ApiKey => inner.ApiKey;
    public string? ApiKeyFile => inner.ApiKeyFile;
    public bool ShowMemories => inner.ShowMemories;
    public bool ShowFavorites => inner.ShowFavorites;
    public bool ShowArchived => inner.ShowArchived;
    public bool ShowVideos => inner.ShowVideos;
    public int? ImagesFromDays => inner.ImagesFromDays;
    public DateTime? ImagesFromDate => inner.ImagesFromDate;
    public DateTime? ImagesUntilDate => inner.ImagesUntilDate;
    public List<Guid> Albums { get; } = albums.Distinct().ToList();
    public List<Guid> ExcludedAlbums => inner.ExcludedAlbums;
    public List<Guid> People => inner.People;
    public List<string> Tags => inner.Tags;
    public int? Rating => inner.Rating;

    // The wrapped settings are already validated.
    public void ValidateAndInitialize() { }
}
