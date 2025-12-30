using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IGeneralSettings _settings;
        private readonly IAccountOverrideStore _overrideStore;

        public ConfigController(ILogger<AssetController> logger, IGeneralSettings settings, IAccountOverrideStore overrideStore)
        {
            _logger = logger;
            _settings = settings;
            _overrideStore = overrideStore;
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

        [HttpGet("account-overrides")]
        public async Task<ActionResult<AccountOverrideDto?>> GetAccountOverrides(CancellationToken ct)
        {
            var dto = await _overrideStore.GetAsync(ct);
            return Ok(dto);
        }

        [Authorize]
        [HttpPut("account-overrides")]
        public async Task<IActionResult> PutAccountOverrides([FromBody] AccountOverrideDto dto, CancellationToken ct)
        {
            if (dto.ImagesFromDays is < 0)
                return BadRequest("ImagesFromDays must be >= 0");

            if (dto.ImagesFromDate.HasValue && dto.ImagesUntilDate.HasValue && dto.ImagesFromDate > dto.ImagesUntilDate)
                return BadRequest("ImagesFromDate must be <= ImagesUntilDate");

            if (dto.Rating is < 0 or > 5)
                return BadRequest("Rating must be between 0 and 5");

            await _overrideStore.UpsertAsync(dto, ct);
            return Ok();
        }
    }
}