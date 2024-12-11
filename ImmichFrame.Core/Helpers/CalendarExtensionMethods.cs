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
            if (occurrence.Source.GetType() == typeof(CalendarEvent)) {
                return ((CalendarEvent)occurrence.Source).ToAppointment();
            }

            return new Appointment
            {
                //Summary = occurrence.Period.Duration.Summary,
                //Description = occurrence.Source.Description,
                StartTime = occurrence.Period.StartTime.AsSystemLocal,
                Duration = occurrence.Period.Duration,
                EndTime = occurrence.Period.EndTime.AsSystemLocal,
                Location = ""
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
