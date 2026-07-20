using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ImmichFrame.Core.Events;

namespace ImmichFrame.WebApi.Models.Events;

public class FrameEventRequestDto
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public FrameEventMode Mode { get; set; }

    public string? Message { get; set; }
    public int? TimeoutMs { get; set; }
    public int Priority { get; set; }
    public string? Category { get; set; }
    public string? Title { get; set; }
    public Dictionary<string, JsonElement>? Meta { get; set; }
    public List<FrameEventActionDto> Actions { get; set; } = new();
    public FrameEventInputDto? Input { get; set; }
    public DateTime? PostedAt { get; set; }

    public FrameEvent ToDomain()
    {
        return new FrameEvent
        {
            DeviceId = DeviceId,
            Id = Id,
            Type = Type,
            Mode = Mode,
            Message = Message,
            TimeoutMs = TimeoutMs,
            Priority = Priority,
            Category = string.IsNullOrWhiteSpace(Category) ? null : Category,
            Title = Title,
            Meta = Meta,
            Actions = Actions.ConvertAll(a => a.ToDomain()),
            Input = (Input ?? new FrameEventInputDto()).ToDomain(),
            PostedAt = PostedAt?.ToUniversalTime() ?? DateTime.UtcNow
        };
    }
}

public class FrameEventActionDto
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Label { get; set; } = string.Empty;

    public string? Kind { get; set; }

    public FrameEventAction ToDomain() => new()
    {
        Id = Id,
        Label = Label,
        Kind = Kind
    };
}

public class FrameEventInputDto
{
    public bool AllowTouchDismiss { get; set; } = true;
    public bool AllowKeyboardDismiss { get; set; } = true;

    public FrameEventInput ToDomain() => new()
    {
        AllowTouchDismiss = AllowTouchDismiss,
        AllowKeyboardDismiss = AllowKeyboardDismiss
    };
}
