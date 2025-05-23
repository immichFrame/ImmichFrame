using System.Net.Http.Headers;
using System.Text;
using Ical.Net;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;
using Microsoft.Extensions.Logging;

public class IcalCalendarService : ICalendarService
{
    private readonly IServerSettings _serverSettings;
    private readonly ILogger<IcalCalendarService> _logger;
    private readonly ApiCache<List<IAppointment>> _appointmentCache = new(TimeSpan.FromMinutes(15));

    public IcalCalendarService(IServerSettings serverSettings, ILogger<IcalCalendarService> logger)
    {
        _logger = logger;
        _serverSettings = serverSettings;
    }

    public async Task<List<IAppointment>> GetAppointments()
    {
        return await _appointmentCache.GetOrAddAsync("appointments", async () =>
        {
            var appointments = new List<IAppointment>();

            List<(string? auth, string url)> cals = _serverSettings.Webcalendars.Select(x => x.Contains(';') ? (x.Split(';')[0], x.Split(';')[1]) : (null, x.ToString())).ToList();

            var icals = await GetCalendars(cals);

            foreach (var ical in icals)
            {
                var calendar = Calendar.Load(ical);

                appointments.AddRange(calendar.GetOccurrences(DateTime.UtcNow, DateTime.Today.AddDays(1)).Select(x => x.ToAppointment()));
            }

            return appointments;
        });
    }

    public async Task<List<string>> GetCalendars(IEnumerable<(string? auth, string url)> calendars)
    {
        var icals = new List<string>();

        using (HttpClient client = new HttpClient())
        {
            foreach (var calendar in calendars)
            {
                _logger.LogDebug($"Loading calendar: {calendar.auth ?? "no auth"} - {calendar.url}");
                client.DefaultRequestHeaders.Authorization = null;

                string httpUrl = calendar.url.Replace("webcal://", "https://");

                if (!string.IsNullOrEmpty(calendar.auth))
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{calendar.auth}");
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                }

                HttpResponseMessage response = await client.GetAsync(httpUrl);
                if (response.IsSuccessStatusCode)
                {
                    icals.Add(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    _logger.LogError($"Failed to load calendar data from '{httpUrl}' (Status: {response.StatusCode})");
                }
            }
        }

        return icals;
    }
}