using System.ComponentModel.DataAnnotations;
using System.Linq;
using ImmichFrame.Core.Events;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models.Events;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IFrameEventQueue _queue;
    private readonly FrameEventValidator _validator;
    private readonly ILogger<EventsController> _logger;
    private readonly IGeneralSettings _settings;

    public EventsController(IFrameEventQueue queue, FrameEventValidator validator, ILogger<EventsController> logger, IGeneralSettings settings)
    {
        _queue = queue;
        _validator = validator;
        _logger = logger;
        _settings = settings;
    }

    [HttpPost]
    public async Task<IActionResult> PostEvent([FromBody] FrameEventRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_settings.EventHostEnabled)
                return NotFound(new { message = "Event host is disabled" });

            var frameEvent = _validator.Validate(request);
            var enqueued = await _queue.EnqueueAsync(frameEvent, cancellationToken);

            if (!enqueued)
                return Conflict(new { message = "Event already exists" });

            return Accepted();
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning(vex, "Invalid frame event received with id {EventId}", Sanitize(request?.Id));
            return BadRequest(new { message = vex.Message });
        }
    }

    [HttpGet("next")]
    public async Task<IActionResult> GetNext([FromQuery] string deviceId, [FromQuery] FrameEventMode? mode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return BadRequest(new { message = "deviceId is required" });

        if (!_settings.EventHostEnabled)
            return NotFound(new { message = "Event host is disabled" });

        var frameEvent = await _queue.PeekNextAsync(deviceId, mode, cancellationToken);

        if (frameEvent is null)
            return NoContent();

        return Ok(FrameEventResponseDto.FromDomain(frameEvent));
    }

    [HttpPost("{eventId}/ack")]
    public async Task<IActionResult> AckEvent(string eventId, [FromQuery] string deviceId, [FromBody] FrameEventAckRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return BadRequest(new { message = "deviceId is required" });

        if (request is null || request.Status is null)
            return BadRequest(new { message = "status is required" });

        if (!_settings.EventHostEnabled)
            return NotFound(new { message = "Event host is disabled" });

        var removed = await _queue.AckAsync(deviceId, eventId, request.Status.Value, cancellationToken);

        if (!removed)
            return NotFound();

        _logger.LogInformation("Acked event {EventId} for {DeviceId} with status {Status}", Sanitize(eventId), Sanitize(deviceId), request.Status);
        return Ok(new { eventId, deviceId, status = request.Status.Value.ToString() });
    }

    private static string Sanitize(string? value)
        => value is null ? "" : value.Replace("\r", "").Replace("\n", "");

    [HttpGet("pending")]
    public IActionResult GetPending([FromQuery] string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return BadRequest(new { message = "deviceId is required" });

        if (!_settings.EventHostEnabled)
            return NotFound(new { message = "Event host is disabled" });

        var snapshot = _queue.GetDeviceSnapshot(deviceId);
        var dto = new FrameEventDiagnosticsDto
        {
            DeviceId = deviceId,
            Pending = snapshot
                .Select(tuple => FrameEventStateDto.From(tuple.Event, tuple.LastAckStatus))
                .ToList()
        };

        return Ok(dto);
    }
}
