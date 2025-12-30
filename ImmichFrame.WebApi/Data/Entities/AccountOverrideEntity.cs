namespace ImmichFrame.WebApi.Data.Entities;

public class AccountOverrideEntity
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    // Nullable means "no override". For lists: null = no override, empty = override to empty.
    public bool? ShowMemories { get; set; }
    public bool? ShowFavorites { get; set; }
    public bool? ShowArchived { get; set; }

    public int? ImagesFromDays { get; set; }
    public DateTime? ImagesFromDate { get; set; }
    public DateTime? ImagesUntilDate { get; set; }

    public List<Guid>? Albums { get; set; }
    public List<Guid>? ExcludedAlbums { get; set; }
    public List<Guid>? People { get; set; }

    public int? Rating { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}


