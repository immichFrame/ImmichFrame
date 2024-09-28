using ImmichFrame.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IClientSettings _settings;

        public ConfigController(ILogger<AssetController> logger, IClientSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        [HttpGet(Name = "GetConfig")]
        public async Task<IClientSettings> GetConfig()
        {
            return _settings;
        }
    }
}
