namespace ImmichFrame.Core.Models;

public record AssetSelection(string AccountName, IReadOnlyList<Guid>? AlbumIds = null);
