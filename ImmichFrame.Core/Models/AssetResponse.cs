namespace ImmichFrame.Core.Models;

public class AssetResponse
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required Stream FileStream { get; init; }
    public string? ContentRange { get; init; }
    public bool IsPartial { get; init; }
    public IDisposable? Owner { get; init; }
    public long? ContentLength { get; init; }
}
