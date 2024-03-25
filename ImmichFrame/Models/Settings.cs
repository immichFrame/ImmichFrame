using ImmichFrame.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ImmichFrame.Models;


public class Settings
{
    public string ImmichServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Interval { get; set; } = 5;
    public bool DownloadImages { get; set; } = false;
    public bool ShowMemories { get; set; } = false;
    public int RenewImagesDuration { get; set; } = 5;
    public List<Guid> Albums { get; set; } = new List<Guid>();
    public List<Guid> People { get; set; } = new List<Guid>();
    public bool ShowClock { get; set; } = false;
    public int ClockFontSize { get; set; } = 5;
    public string? ClockFormat { get; set; }
    public bool ShowPhotoDate { get; set; } = false;
    public int PhotoDateFontSize { get; set; } = 5;
    public string? PhotoDateFormat { get; set; }
    public bool ShowImageDesc { get; set; } = false;
    public int ImageDescFontSize { get; set; } = 5;
    public bool ShowWeather { get; set; } = false;
    public int WeatherFontSize { get; set; } = 5;
    public string? WeatherUnits { get; set; }
    public string? WeatherLatLong { get; set; }

    private static Settings? _settings;
    public static Settings CurrentSettings
    {
        get
        {
            if (_settings == null)
            {
                _settings = Parse();

                _settings.Validate();
            }
            return _settings;
        }
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.ImmichServerUrl))
            throw new SettingsNotValidException($"{nameof(ImmichServerUrl)} is required!");

        if (string.IsNullOrWhiteSpace(this.ApiKey))
            throw new SettingsNotValidException($"{nameof(ApiKey)} is required!");
    }

    private static Settings Parse()
    {
        var xml = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"Settings.xml");

        XElement doc;
        try
        {
            doc = XDocument.Parse(xml).Root!;
        }
        catch (Exception ex)
        {
            throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
        }


        var settings = new Settings();

        foreach (var element in doc.Elements())
        {
            var properties = settings.GetType().GetProperties();

            if(!properties.Select(x => x.Name).Contains(element.Name.LocalName))
                throw new SettingsNotValidException($"Element '{element.Name.LocalName}' is unknown");

            var property = properties.First(x=>x.Name == element.Name.LocalName);

            switch (element.Name.LocalName)
            {
                case "ImmichServerUrl":
                    var url = element.Value.TrimEnd('/');
                    // Match URL or IP
                    if (!Regex.IsMatch(url, @"^(https?:\/\/)?(([a-zA-Z0-9\.\-_]+(\.[a-zA-Z]{2,})+)|(\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b))(\:\d{1,5})?$"))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid");

                    property.SetValue(settings, url);
                    break;
                case "ApiKey":
                    property.SetValue(settings, element.Value);
                    break;
                case "Albums":
                case "People":
                    property.SetValue(settings, element?.DescendantNodes().OfType<XElement>().Select(x => Guid.Parse(x.Value)).Distinct().ToList() ?? new());
                    break;
                case "Interval":
                case "RenewImagesDuration":
                case "ClockFontSize":
                case "PhotoDateFontSize":
                case "ImageDescFontSize":
                case "WeatherFontSize":
                    if (!int.TryParse(element.Value, out var intValue))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid");
                    property.SetValue(settings, intValue);
                    break;
                case "DownloadImages":
                case "ShowMemories":
                case "ShowClock":
                case "ShowPhotoDate":
                case "ShowImageDesc":
                case "ShowWeather":
                    if (!bool.TryParse(element.Value, out var boolValue))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid");
                    property.SetValue(settings, boolValue);
                    break;
                case "ClockFormat":
                    property.SetValue(settings, element.Value);
                    break;
                case "PhotoDateFormat":
                    property.SetValue(settings, element.Value);
                    break;
                case "WeatherUnits":
                    property.SetValue(settings, element.Value);
                    break;
                case "WeatherLatLong":
                    property.SetValue(settings, element.Value);
                    break;
                default:
                    throw new SettingsNotValidException($"Element '{element.Name.LocalName}' is unknown");
            }
        }

        return settings;
    }
}

