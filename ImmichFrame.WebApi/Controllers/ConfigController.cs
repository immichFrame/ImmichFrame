using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IClientSettings _settings;
        private readonly ISettingsProvider _settingsProvider;

        public ConfigController(ILogger<AssetController> logger, IClientSettings settings, ISettingsProvider settingsProvider)
        {
            _logger = logger;
            _settings = settings;
            _settingsProvider = settingsProvider;
        }

        [HttpGet(Name = "GetConfig")]
        public ClientSettingsDto GetConfig(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Config requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            return new ClientSettingsDto(_settings, _settingsProvider.ContentRevision);
        }

        [HttpGet("Version", Name = "GetVersion")]
        public string GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        }
    }
}