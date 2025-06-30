using Ical.Net;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;

public class IcalCalendarService : ICalendarService
{
    private readonly IGeneralSettings _serverSettings;
    private readonly IApiCache _appointmentCache = new ApiCache(TimeSpan.FromMinutes(15));

    public IcalCalendarService(IGeneralSettings serverSettings)
    {
        _serverSettings = serverSettings;
    }

    public async Task<List<IAppointment>> GetAppointments()
    {
        return await _appointmentCache.GetOrAddAsync("appointments", async () =>
        {
            var appointments = new List<IAppointment>();

            var icals = await GetCalendars(_serverSettings.Webcalendars);

            foreach (var ical in icals)
            {
                var calendar = Calendar.Load(ical);

                appointments.AddRange(calendar.GetOccurrences(DateTime.UtcNow, DateTime.Today.AddDays(1)).Select(x => x.ToAppointment()));
            }

            return appointments;
        });
    }

    public async Task<List<string>> GetCalendars(IEnumerable<string> calendars)
    {
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

        return icals;
    }

}