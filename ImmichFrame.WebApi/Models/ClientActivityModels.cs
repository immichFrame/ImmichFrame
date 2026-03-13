namespace ImmichFrame.WebApi.Models;

public sealed class AssetRequestRecord
{
    public required Guid AssetId { get; init; }
    public required string ClientIdentifier { get; init; }
    public required DateTimeOffset RequestedAtUtc { get; init; }
    public string? Endpoint { get; init; }
    public string? AssetType { get; init; }
}

public sealed class RecentAssetRequestDetailsRecord
{
    public required Guid AssetId { get; init; }
    public required string ClientIdentifier { get; init; }
    public required DateTimeOffset RequestedAtUtc { get; init; }
    public string? Endpoint { get; init; }
    public string? AssetType { get; init; }
    public string? OriginalFileName { get; init; }
    public DateTimeOffset? TakenAtUtc { get; init; }
    public string? Location { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
}
