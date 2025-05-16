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
        private readonly IWebClientSettings _settings;

        public ConfigController(ILogger<AssetController> logger, IWebClientSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        [HttpGet(Name = "GetConfig")]
        public WebClientSettings GetConfig(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Config requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            return (WebClientSettings)_settings;
        }

        [HttpGet("Version", Name = "GetVersion")]
        public string GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        }
    }
}
