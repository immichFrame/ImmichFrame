using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IGeneralSettings _settings;
        private readonly IAccountOverrideStore _overrideStore;
        private readonly IAccountOverrideChangeNotifier _overrideChangeNotifier;

        public ConfigController(
            ILogger<AssetController> logger,
            IGeneralSettings settings,
            IAccountOverrideStore overrideStore,
            IAccountOverrideChangeNotifier overrideChangeNotifier)
        {
            _logger = logger;
            _settings = settings;
            _overrideStore = overrideStore;
            _overrideChangeNotifier = overrideChangeNotifier;
        }

        [HttpGet(Name = "GetConfig")]
        public ClientSettingsDto GetConfig(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Config requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            return ClientSettingsDto.FromGeneralSettings(_settings);
        }

        [HttpGet("Version", Name = "GetVersion")]
        public string GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        }

        [HttpGet("account-overrides", Name = "GetAccountOverrides")]
        public async Task<ActionResult<AccountOverrideDto?>> GetAccountOverrides(CancellationToken ct)
        {
            var dto = await _overrideStore.GetAsync(ct);
            return Ok(dto);
        }

        [HttpGet("account-overrides/version", Name = "GetAccountOverridesVersion")]
        public async Task<ActionResult<long>> GetAccountOverridesVersion(CancellationToken ct)
        {
            var version = await _overrideStore.GetVersionAsync(ct);
            return Ok(version);
        }

        /// <summary>
        /// Server-Sent Events stream: emits the override "version" (ticks) whenever account overrides change.
        /// </summary>
        [HttpGet("account-overrides/events", Name = "GetAccountOverridesEvents")]
        public async Task GetAccountOverridesEvents(CancellationToken ct)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("X-Accel-Buffering", "no");

            // Send current version immediately (helps clients initialize).
            var initialVersion = await _overrideStore.GetVersionAsync(ct);
            await WriteSseAsync("account-overrides", initialVersion.ToString(), ct);

            var reader = _overrideChangeNotifier.Subscribe(ct);
            while (!ct.IsCancellationRequested && await reader.WaitToReadAsync(ct))
            {
                while (reader.TryRead(out var version))
                {
                    await WriteSseAsync("account-overrides", version.ToString(), ct);
                }
            }
        }

        [Authorize]
        [HttpPut("account-overrides", Name = "PutAccountOverrides")]
        public async Task<IActionResult> PutAccountOverrides([FromBody] AccountOverrideDto dto, CancellationToken ct)
        {
            if (dto.ImagesFromDays is < 0)
                return BadRequest("ImagesFromDays must be >= 0");

            if (dto.ImagesFromDate.HasValue && dto.ImagesUntilDate.HasValue && dto.ImagesFromDate > dto.ImagesUntilDate)
                return BadRequest("ImagesFromDate must be <= ImagesUntilDate");

            if (dto.Rating is < 0 or > 5)
                return BadRequest("Rating must be between 0 and 5");

            await _overrideStore.UpsertAsync(dto, ct);
            var version = await _overrideStore.GetVersionAsync(ct);
            _overrideChangeNotifier.NotifyChanged(version);
            return Ok();
        }

        private async Task WriteSseAsync(string eventName, string data, CancellationToken ct)
        {
            // SSE format:
            // event: <name>
            // data: <payload>
            // \n
            var payload = $"event: {eventName}\n" +
                          $"data: {data}\n\n";
            var bytes = Encoding.UTF8.GetBytes(payload);
            await Response.Body.WriteAsync(bytes, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}