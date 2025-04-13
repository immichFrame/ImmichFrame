using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
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
            return (WebClientSettings)_settings;
        }
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            var version = System.Reflection.Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version?.ToString() ?? "unknown";

            return Ok(new { version });
        }
    }
}
