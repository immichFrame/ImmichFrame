using System.Text.Json.Serialization;
using ImmichFrame.Core.Api;

namespace ImmichFrame.WebApi.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FramePlaybackState
{
    Playing,
    Paused
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FrameSessionStatus
{
    Active,
    Stopped
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FrameAdminCommandType
{
    Previous,
    Play,
    Pause,
    Next,
    Refresh,
    Shutdown
}

public class DisplayedAssetDto
{
    public required string Id { get; set; }
    public required string OriginalFileName { get; set; }
    public AssetTypeEnum Type { get; set; }
    public string? ImmichServerUrl { get; set; }
    public string? LocalDateTime { get; set; }
    public string? Description { get; set; }
    public string? Thumbhash { get; set; }
}

public class DisplayEventDto
{
    public DateTimeOffset DisplayedAtUtc { get; set; }
    public double? DurationSeconds { get; set; }
    public List<DisplayedAssetDto> Assets { get; set; } = new();
}

public class FrameSessionSnapshotDto
{
    public FramePlaybackState PlaybackState { get; set; } = FramePlaybackState.Playing;
    public FrameSessionStatus Status { get; set; } = FrameSessionStatus.Active;
    public string? DisplayName { get; set; }
    public DisplayEventDto? CurrentDisplay { get; set; }
    public List<DisplayEventDto> History { get; set; } = new();
}

public class FrameSessionStateDto : FrameSessionSnapshotDto
{
    public required string ClientIdentifier { get; set; }
    public DateTimeOffset ConnectedAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public string? UserAgent { get; set; }
}

public class AdminCommandDto
{
    public long CommandId { get; set; }
    public FrameAdminCommandType CommandType { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
}

public class EnqueueAdminCommandRequest
{
    public FrameAdminCommandType CommandType { get; set; }
}

public class UpdateFrameSessionDisplayNameRequest
{
    public string? DisplayName { get; set; }
}
