using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WeatherController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IWeatherService _weatherService;

        public WeatherController(ILogger<AssetController> logger, IWeatherService weatherService)
        {
            _logger = logger;
            _weatherService = weatherService;
        }

        [HttpGet(Name = "GetWeather")]
        public async Task<IWeather?> GetWeather(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = ClientIdentifier.Sanitize(clientIdentifier);
            _logger.LogDebug("Weather requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            return await _weatherService.GetWeather();
        }
    }
}
