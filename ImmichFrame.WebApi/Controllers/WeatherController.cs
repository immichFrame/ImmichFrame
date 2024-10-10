using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using Microsoft.AspNetCore.Mvc;
using OpenWeatherMap.Models;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly IImmichFrameLogic _logic;

        public WeatherController(ILogger<AssetController> logger, ImmichFrameLogic logic)
        {
            _logger = logger;
            _logic = logic;
        }

        [HttpGet(Name = "GetWeather")]
        public async Task<IWeather?> GetWeather()
        {
            return await _logic.GetWeather();
        }
    }
}
