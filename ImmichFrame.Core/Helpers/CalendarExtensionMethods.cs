using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;
namespace ImmichFrame.WebApi.Helpers
{
    public static class CalendarExtensionMethods
    {
        public static IAppointment ToAppointment(this Occurrence occurrence)
        {
            string summary = "";
            string? description = null;
            string? location = null;

            if (occurrence.Source is CalendarEvent calEvent)
            {
                summary = calEvent.Summary;
                description = calEvent.Description;
                location = calEvent.Location;
            }

            return new Appointment
            {
                Summary = summary,
                Description = description,
                StartTime = occurrence.Period.StartTime.AsSystemLocal,
                Duration = occurrence.Period.Duration,
                EndTime = occurrence.Period.EndTime.AsSystemLocal,
                Location = location
            };
        }
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
