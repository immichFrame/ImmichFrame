using Ical.Net;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;

public class IcalCalendarService : ICalendarService
{
    private readonly IServerSettings _serverSettings;

    public IcalCalendarService(IServerSettings serverSettings)
    {
        _serverSettings = serverSettings;
    }

    public async Task<List<IAppointment>> GetAppointments()
    {
        var appointments = new List<IAppointment>();

        var icals = await GetCalendars(_serverSettings.Webcalendars);

        foreach (var ical in icals)
        {
            var calendar = Calendar.Load(ical);

            appointments.AddRange(calendar.GetOccurrences(DateTime.UtcNow, DateTime.Today.AddDays(1)).Select(x => x.ToAppointment()));
        }

        return appointments;
    }

    private (DateTime fetchDate, List<string> calendars)? lastCalendars;
    public async Task<List<string>> GetCalendars(IEnumerable<string> calendars)
    {
        if (lastCalendars != null && lastCalendars.Value.fetchDate.AddMinutes(15) > DateTime.Now)
        {
            return lastCalendars.Value.calendars;
        }

        var icals = new List<string>();

        foreach (var webcal in calendars)
        {
            string httpUrl = webcal.Replace("webcal://", "https://");

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(httpUrl);
                if (response.IsSuccessStatusCode)
                {
                    icals.Add(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    throw new Exception("Failed to load calendar data");
                }
            }
        }

        lastCalendars = (DateTime.Now, icals);

        return icals;
    }

}