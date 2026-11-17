using System;
using System.Collections.Generic;
using System.Text.Json;
using ImmichFrame.Core.Events;

namespace ImmichFrame.WebApi.Models.Events;

public class FrameEventResponseDto
{
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

    public static FrameEventResponseDto FromDomain(FrameEvent frameEvent) => new()
    {
        Id = frameEvent.Id,
        Type = frameEvent.Type,
        Mode = frameEvent.Mode,
        Message = frameEvent.Message,
        TimeoutMs = frameEvent.TimeoutMs,
        Priority = frameEvent.Priority,
        Category = frameEvent.Category,
        Title = frameEvent.Title,
        Meta = frameEvent.Meta,
        Actions = frameEvent.Actions,
        Input = frameEvent.Input,
        PostedAt = frameEvent.PostedAt
    };
}
