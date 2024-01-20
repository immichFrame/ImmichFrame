using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImmichFrame;
static class Weather
{
    public static async Task<OpenMeteoResponse?> GetWeather(string latitude, string longitude, string temperatureUnit)
    {
        OpenMeteoResponse? openMeteoResponse = null;
        string apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true&hourly=temperature_2m&temperature_unit={temperatureUnit}";
        using (var httpClient = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    openMeteoResponse = JsonSerializer.Deserialize<OpenMeteoResponse>(json);
                }
                //else
                //{
                //    throw new Exception($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                //}
            }
            catch 
            {
                //throw new Exception($"Exception: {ex.Message}");
            }
            return openMeteoResponse;
        }
    }
}

public class CurrentWeather
    {
        public string? time { get; set; }
        public int interval { get; set; }
        public double temperature { get; set; }
        public double windspeed { get; set; }
        public int winddirection { get; set; }
        public int is_day { get; set; }
        public int weathercode { get; set; }
    }

    public class CurrentWeatherUnits
    {
        public string? time { get; set; }
        public string? interval { get; set; }
        public string? temperature { get; set; }
        public string? windspeed { get; set; }
        public string? winddirection { get; set; }
        public string? is_day { get; set; }
        public string? weathercode { get; set; }
    }

    public class Hourly
    {
        public List<string>? time { get; set; }
        public List<double>? temperature_2m { get; set; }
    }

    public class HourlyUnits
    {
        public string? time { get; set; }
        public string? temperature_2m { get; set; }
    }

    public class OpenMeteoResponse
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double generationtime_ms { get; set; }
        public int utc_offset_seconds { get; set; }
        public string? timezone { get; set; }
        public string? timezone_abbreviation { get; set; }
        public double elevation { get; set; }
        public CurrentWeatherUnits? current_weather_units { get; set; }
        public CurrentWeather? current_weather { get; set; }
        public HourlyUnits? hourly_units { get; set; }
        public Hourly? hourly { get; set; }
    }