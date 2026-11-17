using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ImmichFrame.Core.Events;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models.Events;

namespace ImmichFrame.WebApi.Services;

public class FrameEventValidator
{
    private readonly IGeneralSettings _settings;

    public FrameEventValidator(IGeneralSettings settings)
    {
        _settings = settings;
    }

    public FrameEvent Validate(FrameEventRequestDto dto)
    {
        if (dto is null)
            throw new ValidationException("request body is required");

        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            throw new ValidationException("deviceId is required");

        if (string.IsNullOrWhiteSpace(dto.Id))
            throw new ValidationException("id is required");

        if (string.IsNullOrWhiteSpace(dto.Type))
            throw new ValidationException("type is required");

        if (!dto.Type.StartsWith("frame.ui.", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("type must start with 'frame.ui.'");

        var timeoutMs = dto.TimeoutMs ?? _settings.EventDefaultTimeoutMs;
        if (timeoutMs < 0)
            throw new ValidationException("timeoutMs must be >= 0");

        switch (dto.Mode)
        {
            case FrameEventMode.PopupText:
                if (string.IsNullOrWhiteSpace(dto.Message))
                    throw new ValidationException("message is required for PopupText mode");
                break;

            case FrameEventMode.Banner:
                if (string.IsNullOrWhiteSpace(dto.Message))
                    throw new ValidationException("message is required for Banner mode");
                break;

            case FrameEventMode.Close:
                break;

            default:
                throw new ValidationException($"mode '{dto.Mode}' is not supported");
        }

        IReadOnlyList<FrameEventAction> actions = dto.Actions?.Count > 0
            ? dto.Actions.ConvertAll(a => a.ToDomain())
            : Array.Empty<FrameEventAction>();

        var input = dto.Input?.ToDomain() ?? new FrameEventInput();

        return new FrameEvent
        {
            DeviceId = dto.DeviceId,
            Id = dto.Id,
            Type = dto.Type,
            Mode = dto.Mode,
            Message = dto.Message,
            TimeoutMs = timeoutMs,
            Priority = dto.Priority,
            Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category,
            Title = dto.Title,
            Meta = dto.Meta,
            Actions = actions,
            Input = input,
            PostedAt = dto.PostedAt?.ToUniversalTime() ?? DateTime.UtcNow
        };
    }
}
