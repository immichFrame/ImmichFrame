using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ImmichFrame.Core.Events;

public class FrameEvent
{
    public string DeviceId { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public FrameEventMode Mode { get; init; }
    public string? Message { get; init; }
    public int? TimeoutMs { get; init; }
    public int Priority { get; init; }
    public string? Category { get; init; }
    public string? Title { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? Meta { get; init; }
    public IReadOnlyList<FrameEventAction> Actions { get; init; } = Array.Empty<FrameEventAction>();
    public FrameEventInput Input { get; init; } = new();
    public DateTime PostedAt { get; init; }
}
