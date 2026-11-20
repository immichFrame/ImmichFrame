using Ical.Net;
using ImmichFrame.WebApi.Helpers;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Helpers;

[TestFixture]
public class CalendarExtensionMethodsTests
{
    // Pinned zone so the results don't depend on the machine running the tests (CI runs in UTC).
    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    // Friday 2026-11-06 (CST, UTC-6).
    private static readonly DateTime Today = new(2026, 11, 6);

    private static Calendar Load(params string[] events)
    {
        var ics = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//test//EN\r\n"
                  + string.Join("", events.Select(e => "BEGIN:VEVENT\r\n" + e.Replace("\n", "\r\n").Trim() + "\r\nEND:VEVENT\r\n"))
                  + "END:VCALENDAR\r\n";
        return Calendar.Load(ics);
    }

    [Test]
    public void YearlyAllDayEvent_ReportsThisYearsOccurrence_NotSeriesStart()
    {
        var calendar = Load("""
            UID:bday
            DTSTART;VALUE=DATE:20221106
            DTEND;VALUE=DATE:20221107
            RRULE:FREQ=YEARLY
            SUMMARY:Birthday
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Summary, Is.EqualTo("Birthday"));
        Assert.That(result[0].StartTime, Is.EqualTo(new DateTime(2026, 11, 6)));
        Assert.That(result[0].EndTime, Is.EqualTo(new DateTime(2026, 11, 7)));
        Assert.That(result[0].Duration, Is.EqualTo(TimeSpan.FromDays(1)));
    }

    [Test]
    public void BiweeklyAllDayEvent_ReportsTodaysOccurrence()
    {
        var calendar = Load("""
            UID:payday
            DTSTART;VALUE=DATE:20230721
            DTEND;VALUE=DATE:20230722
            RRULE:FREQ=WEEKLY;INTERVAL=2
            SUMMARY:Payday
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].StartTime, Is.EqualTo(new DateTime(2026, 11, 6)));
    }

    [Test]
    public void RecurringTimedEvent_InZone_ReportsTodaysOccurrenceAtLocalTime()
    {
        // Weekly on Fridays at 9:00 am Chicago time, starting in summer (CDT) - today is CST.
        var calendar = Load("""
            UID:standup
            DTSTART;TZID=America/Chicago:20260703T090000
            DTEND;TZID=America/Chicago:20260703T093000
            RRULE:FREQ=WEEKLY
            SUMMARY:Standup
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].StartTime, Is.EqualTo(new DateTime(2026, 11, 6, 9, 0, 0)));
        Assert.That(result[0].EndTime, Is.EqualTo(new DateTime(2026, 11, 6, 9, 30, 0)));
    }

    [Test]
    public void EventYesterdayEvening_IsNotIncluded()
    {
        // 2026-11-05 7:45 pm CST - after midnight UTC on the 6th, but yesterday locally.
        var calendar = Load("""
            UID:appt
            DTSTART:20261106T014500Z
            DTEND:20261106T024500Z
            SUMMARY:Appointment
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void EventThisEvening_IsIncluded_AtLocalTime()
    {
        // 2026-11-06 7:45 pm CST = 2026-11-07 01:45 UTC.
        var calendar = Load("""
            UID:dinner
            DTSTART:20261107T014500Z
            DTEND:20261107T024500Z
            SUMMARY:Dinner
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].StartTime, Is.EqualTo(new DateTime(2026, 11, 6, 19, 45, 0)));
    }

    [Test]
    public void AllDayEventsOnNeighbouringDays_AreNotIncluded()
    {
        var calendar = Load("""
            UID:yesterday
            DTSTART;VALUE=DATE:20261105
            DTEND;VALUE=DATE:20261106
            SUMMARY:Yesterday
            """, """
            UID:tomorrow
            DTSTART;VALUE=DATE:20261107
            DTEND;VALUE=DATE:20261108
            SUMMARY:Tomorrow
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void MultiDayAllDayEvent_CoveringToday_IsIncluded()
    {
        var calendar = Load("""
            UID:trip
            DTSTART;VALUE=DATE:20261105
            DTEND;VALUE=DATE:20261108
            SUMMARY:Trip
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].StartTime, Is.EqualTo(new DateTime(2026, 11, 5)));
        Assert.That(result[0].EndTime, Is.EqualTo(new DateTime(2026, 11, 8)));
    }

    [Test]
    public void EventSpanningMidnightIntoToday_IsIncluded()
    {
        // 2026-11-05 11:00 pm CST to 2026-11-06 1:00 am CST.
        var calendar = Load("""
            UID:late
            DTSTART:20261106T050000Z
            DTEND:20261106T070000Z
            SUMMARY:Late show
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].StartTime, Is.EqualTo(new DateTime(2026, 11, 5, 23, 0, 0)));
    }

    [Test]
    public void Results_AreOrderedByStartTime()
    {
        var calendar = Load("""
            UID:b
            DTSTART;TZID=America/Chicago:20261106T150000
            DTEND;TZID=America/Chicago:20261106T160000
            SUMMARY:Afternoon
            """, """
            UID:a
            DTSTART;TZID=America/Chicago:20261106T080000
            DTEND;TZID=America/Chicago:20261106T090000
            SUMMARY:Morning
            """);

        var result = calendar.GetAppointmentsForDay(Today, Chicago);

        Assert.That(result.Select(a => a.Summary), Is.EqualTo(new[] { "Morning", "Afternoon" }));
    }
}
