using System.Net.Http.Headers;
using System.Text;
using Ical.Net;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;
using Microsoft.Extensions.Logging;

public class IcalCalendarService : ICalendarService
{
    private readonly IGeneralSettings _serverSettings;
    private readonly ILogger<IcalCalendarService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiCache _appointmentCache = new(TimeSpan.FromMinutes(15));

    public IcalCalendarService(IGeneralSettings serverSettings, ILogger<IcalCalendarService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _serverSettings = serverSettings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<IAppointment>> GetAppointments()
    {
        return await _appointmentCache.GetOrAddAsync("appointments", async () =>
        {
            var appointments = new List<IAppointment>();

            List<(string? auth, string url)> cals = _serverSettings.Webcalendars.Select<string, (string? auth, string url)?>(x =>
            {
                try
                {
                    var uri = new Uri(x.Replace("webcal://", "https://"));
                    if (!string.IsNullOrEmpty(uri.UserInfo))
                    {
                        var url = uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped);
                        return (Uri.UnescapeDataString(uri.UserInfo), url);
                    }
                    return (null, x);
                }
                catch (UriFormatException)
                {
                    _logger.LogError($"Invalid calendar URL: '{x}'");
                    return null;
                }
            }).Where(x => x != null).Select(x => x!.Value).ToList();

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
        var client = _httpClientFactory.CreateClient();

        foreach (var calendar in calendars)
        {
            _logger.LogDebug($"Loading calendar: {(calendar.auth != null ? "[authenticated]" : "no auth")} - {calendar.url}");

            string httpUrl = calendar.url.Replace("webcal://", "https://");

            var request = new HttpRequestMessage(HttpMethod.Get, httpUrl);

            if (!string.IsNullOrEmpty(calendar.auth))
            {
                var byteArray = Encoding.UTF8.GetBytes(calendar.auth);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                icals.Add(await response.Content.ReadAsStringAsync());
            }
            else
            {
                _logger.LogError($"Failed to load calendar data from '{httpUrl}' (Status: {response.StatusCode})");
            }
        }

        return icals;
    }
}