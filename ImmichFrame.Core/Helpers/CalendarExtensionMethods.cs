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
                StartTime = calEvent.Start.AsSystemLocal,
                Duration = calEvent.Duration,
                EndTime = calEvent.End.AsSystemLocal,
                Location = calEvent.Location
            };
        }
    }
}
