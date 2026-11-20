﻿using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Models;

namespace ImmichFrame.WebApi.Helpers
{
    public static class CalendarExtensionMethods
    {
        /// <summary>
        /// Returns the occurrences that overlap the given calendar day in <paramref name="timeZone"/>,
        /// with start/end times expressed as wall-clock times in that zone.
        /// </summary>
        public static List<IAppointment> GetAppointmentsForDay(this Calendar calendar, DateTime day, TimeZoneInfo timeZone)
        {
            var dayStart = day.Date;
            var dayEnd = dayStart.AddDays(1);

            // Query a padded floating window so no candidate is missed whatever the zone offset
            // (or how Ical.Net interprets an unzoned DateTime), then filter exactly on local times.
            var candidates = calendar.GetOccurrences(
                new CalDateTime(dayStart.AddDays(-2)),
                new CalDateTime(dayEnd.AddDays(2)));

            return candidates
                .Select(o => o.ToAppointment(timeZone))
                .Where(a => a.StartTime < dayEnd && (a.EndTime > dayStart || a.StartTime >= dayStart))
                .OrderBy(a => a.StartTime)
                .ToList();
        }

        /// <summary>
        /// Converts one occurrence to an appointment. The times come from the occurrence's own
        /// period, so recurring events report this occurrence's date rather than the series' DTSTART.
        /// </summary>
        public static IAppointment ToAppointment(this Occurrence occurrence, TimeZoneInfo timeZone)
        {
            var period = occurrence.Period;
            var start = ToWallClock(period.StartTime, timeZone);
            DateTime end;
            if (!period.StartTime.HasTime)
            {
                // All-day: span whole calendar days. Ical.Net derives the occurrence end from the
                // series' elapsed duration, which is off by an hour if the first occurrence fell on
                // a DST change (e.g. a yearly event first entered on the day DST ended).
                end = DateTime.SpecifyKind(start.Date.AddDays(AllDaySpanInDays(occurrence.Source)), start.Kind);
            }
            else
            {
                end = period.EndTime != null
                    ? ToWallClock(period.EndTime, timeZone)
                    : start.Add(period.Duration);
            }

            var appointment = new Appointment
            {
                StartTime = start,
                EndTime = end,
                Duration = end - start,
            };

            if (occurrence.Source is CalendarEvent calEvent)
            {
                appointment.Summary = calEvent.Summary ?? "";
                appointment.Description = calEvent.Description ?? "";
                appointment.Location = calEvent.Location ?? "";
            }

            return appointment;
        }

        public static IAppointment ToAppointment(this Occurrence occurrence)
            => occurrence.ToAppointment(TimeZoneInfo.Local);

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

        private static int AllDaySpanInDays(object? source)
        {
            if (source is CalendarEvent { DtStart: not null, DtEnd: not null } calEvent)
            {
                var days = (calEvent.DtEnd.Value.Date - calEvent.DtStart.Value.Date).Days;
                if (days > 0)
                {
                    return days;
                }
            }

            // RFC 5545: an all-day event without DTEND lasts one day.
            return 1;
        }

        /// <summary>
        /// Wall-clock time of <paramref name="value"/> in <paramref name="timeZone"/>.
        /// UTC and zoned values are converted; floating values (including all-day dates)
        /// already are wall-clock times and are kept as-is.
        /// </summary>
        private static DateTime ToWallClock(IDateTime value, TimeZoneInfo timeZone)
        {
            DateTime wallClock;
            if (value.IsUtc || !string.IsNullOrEmpty(value.TzId))
            {
                wallClock = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value.AsUtc, DateTimeKind.Utc), timeZone);
            }
            else
            {
                wallClock = value.Value;
            }

            // Keep the API's existing "local time with offset" serialization when using the server's zone.
            var kind = timeZone.Equals(TimeZoneInfo.Local) ? DateTimeKind.Local : DateTimeKind.Unspecified;
            return DateTime.SpecifyKind(wallClock, kind);
        }
    }
}
