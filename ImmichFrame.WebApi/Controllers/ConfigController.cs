using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
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
            _logger.LogTrace("Config requested by '{ClientIdentifier}'", sanitizedClientIdentifier);
            return (WebClientSettings)_settings;
        }

        [HttpGet("GetVersion")]
        public string GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        }
    }
}
