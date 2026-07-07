using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomWidgetController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly ICustomWidgetService _customWidgetService;

        public CustomWidgetController(ILogger<AssetController> logger, ICustomWidgetService customWidgetService)
        {
            _logger = logger;
            _customWidgetService = customWidgetService;
        }

        [HttpGet(Name = "GetCustomWidgetData")]
        public async Task<List<CustomWidgetData>> GetCustomWidgetData(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Custom widget data requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            return await _customWidgetService.GetCustomWidgetData();
        }
    }
}
