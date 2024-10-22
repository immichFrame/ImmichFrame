using Ical.Net.CalendarComponents;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;

namespace ImmichFrame.WebApi.Helpers
{
    public static class CalendarExtensionMethods
    {
        public static IAppointment ToAppointment(this CalendarEvent calEvent)
        {
            return new Appointment
            {
                Summary = calEvent.Summary,
                Description = calEvent.Description,
                StartTime = calEvent.Start.AsSystemLocal,
                Duration = calEvent.Duration,
                EndTime = calEvent.End.AsSystemLocal,
                Location = calEvent.Location
            };
        }
    }
}
