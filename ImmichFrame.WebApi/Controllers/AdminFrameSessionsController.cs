using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers;

[ApiController]
[Route("api/admin/frame-sessions")]
[Authorize(AuthenticationSchemes = AuthSchemes.Admin)]
public class AdminFrameSessionsController : ControllerBase
{
    private readonly ILogger<AdminFrameSessionsController> _logger;
    private readonly IFrameSessionRegistry _registry;

    public AdminFrameSessionsController(ILogger<AdminFrameSessionsController> logger, IFrameSessionRegistry registry)
    {
        _logger = logger;
        _registry = registry;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<FrameSessionStateDto>> GetActiveSessions()
    {
        return Ok(_registry.GetActiveSessions());
    }

    [HttpPost("{clientIdentifier}/commands")]
    public ActionResult<AdminCommandDto> EnqueueCommand(string clientIdentifier, [FromBody] EnqueueAdminCommandRequest request)
    {
        var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
        if (string.IsNullOrWhiteSpace(sanitizedClientIdentifier))
        {
            return BadRequest("A valid client identifier is required.");
        }

        var result = _registry.EnqueueCommand(sanitizedClientIdentifier, request.CommandType);
        if (result.Status == FrameSessionCommandEnqueueStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == FrameSessionCommandEnqueueStatus.Stale)
        {
            return Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Frame session is stale.",
                detail: "The frame session is no longer active and cannot receive commands.");
        }

        var command = result.Command!;
        _logger.LogInformation("Queued {commandType} command for frame session '{clientIdentifier}'", request.CommandType, sanitizedClientIdentifier);
        return Ok(command);
    }

    [HttpPut("{clientIdentifier}/display-name")]
    public IActionResult UpdateDisplayName(string clientIdentifier, [FromBody] UpdateFrameSessionDisplayNameRequest request)
    {
        var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
        if (string.IsNullOrWhiteSpace(sanitizedClientIdentifier))
        {
            return BadRequest("A valid client identifier is required.");
        }

        return _registry.UpdateDisplayName(sanitizedClientIdentifier, request.DisplayName)
            ? NoContent()
            : NotFound();
    }
}
