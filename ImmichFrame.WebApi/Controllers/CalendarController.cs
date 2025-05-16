using ImmichFrame.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly ILogger<AssetController> _logger;
        private readonly ICalendarService _calendarService;

        public CalendarController(ILogger<AssetController> logger, ICalendarService calendarService)
        {
            _logger = logger;
            _calendarService = calendarService;
        }

        [HttpGet(Name = "GetAppointments")]
        public async Task<List<IAppointment>> GetAppointments(string clientIdentifier = "")
        {
            var sanitizedClientIdentifier = clientIdentifier.SanitizeString();
            _logger.LogDebug("Calendar requested by '{sanitizedClientIdentifier}'", sanitizedClientIdentifier);
            return await _calendarService.GetAppointments();
        }
    }
}
