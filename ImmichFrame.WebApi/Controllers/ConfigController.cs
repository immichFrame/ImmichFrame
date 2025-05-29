using ImmichFrame.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IGeneralSettings _settings;

        public ConfigController(ILogger<AssetController> logger, IGeneralSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        [HttpGet(Name = "GetConfig")]
        public IGeneralSettings GetConfig(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Config requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            return _settings;
        }

        [HttpGet("Version", Name = "GetVersion")]
        public string GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        }
    }
}
