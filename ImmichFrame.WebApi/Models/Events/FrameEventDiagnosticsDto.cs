using System.Collections.Generic;
using ImmichFrame.Core.Events;

namespace ImmichFrame.WebApi.Models.Events;

public class FrameEventDiagnosticsDto
{
    public string DeviceId { get; init; } = string.Empty;
    public IReadOnlyList<FrameEventStateDto> Pending { get; init; } = new List<FrameEventStateDto>();
}

public class FrameEventStateDto
{
    public string Id { get; init; } = string.Empty;
    public FrameEventMode Mode { get; init; }
    public string? Category { get; init; }
    public int Priority { get; init; }
    public FrameEventAckStatus? LastAckStatus { get; init; }
    public string? Title { get; init; }
    public int? TimeoutMs { get; init; }
    public string? PostedAt { get; init; }

    public static FrameEventStateDto From(FrameEvent frameEvent, FrameEventAckStatus? ackStatus)
    {
        return new FrameEventStateDto
        {
            Id = frameEvent.Id,
            Mode = frameEvent.Mode,
            Category = frameEvent.Category,
            Priority = frameEvent.Priority,
            LastAckStatus = ackStatus,
            Title = frameEvent.Title,
            TimeoutMs = frameEvent.TimeoutMs,
            PostedAt = frameEvent.PostedAt.ToString("o")
        };
    }
}
