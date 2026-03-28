using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers;

[ApiController]
[Route("api/frame-sessions")]
[Authorize(AuthenticationSchemes = AuthSchemes.Frame)]
public class FrameSessionsController : ControllerBase
{
    private readonly ILogger<FrameSessionsController> _logger;
    private readonly IFrameSessionRegistry _registry;

    public FrameSessionsController(ILogger<FrameSessionsController> logger, IFrameSessionRegistry registry)
    {
        _logger = logger;
        _registry = registry;
    }

    [HttpPut("{clientIdentifier}")]
    public IActionResult UpsertSessionSnapshot(string clientIdentifier, [FromBody] FrameSessionSnapshotDto snapshot)
    {
        var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
        if (string.IsNullOrWhiteSpace(sanitizedClientIdentifier))
        {
            return BadRequest("A valid client identifier is required.");
        }

        _logger.LogDebug("Frame session snapshot received for '{clientIdentifier}'", sanitizedClientIdentifier);
        _registry.UpsertSnapshot(sanitizedClientIdentifier, snapshot, Request.Headers.UserAgent.ToString());
        return NoContent();
    }

    [HttpGet("{clientIdentifier}/commands")]
    public ActionResult<IReadOnlyList<AdminCommandDto>> GetPendingCommands(string clientIdentifier)
    {
        var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
        if (string.IsNullOrWhiteSpace(sanitizedClientIdentifier))
        {
            return BadRequest("A valid client identifier is required.");
        }

        var commands = _registry.GetPendingCommands(sanitizedClientIdentifier);
        return Ok(commands);
    }

    [HttpPost("{clientIdentifier}/commands/{commandId:long}/ack")]
    public IActionResult AcknowledgeCommand(string clientIdentifier, long commandId)
    {
        var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
        if (string.IsNullOrWhiteSpace(sanitizedClientIdentifier))
        {
            return BadRequest("A valid client identifier is required.");
        }

        return _registry.AcknowledgeCommand(sanitizedClientIdentifier, commandId)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("{clientIdentifier}/disconnect")]
    public IActionResult Disconnect(string clientIdentifier)
    {
        var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
        if (string.IsNullOrWhiteSpace(sanitizedClientIdentifier))
        {
            return BadRequest("A valid client identifier is required.");
        }

        _logger.LogDebug("Frame session disconnect received for '{clientIdentifier}'", sanitizedClientIdentifier);
        _registry.MarkStopped(sanitizedClientIdentifier, Request.Headers.UserAgent.ToString());
        return NoContent();
    }
}
