using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
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
    public int Interval { get; set; } = 8;
    public TimeSpan TransitionDuration { get; set; } = TimeSpan.FromSeconds(1);
    public bool DownloadImages { get; set; } = false;
    public bool ShowMemories { get; set; } = false;
    public int RenewImagesDuration { get; set; } = 20;
    public List<Guid> Albums { get; set; } = new List<Guid>();
    public List<Guid> People { get; set; } = new List<Guid>();
    public bool ShowClock { get; set; } = true;
    public int ClockFontSize { get; set; } = 48;
    public string? ClockFormat { get; set; } = "h:mm tt";
    public bool ShowPhotoDate { get; set; } = true;
    public int PhotoDateFontSize { get; set; } = 36;
    public string? PhotoDateFormat { get; set; } = "MM/dd/yyyy";
    public bool ShowImageDesc { get; set; } = true;
    public int ImageDescFontSize { get; set; } = 36;
    public bool ShowWeather { get; set; } = false;
    public int WeatherFontSize { get; set; } = 36;
    public string? WeatherUnits { get; set; } = "fahrenheit";
    public string? WeatherLatLong { get; set; } = "40.7128,74.0060";

    private static Settings? _settings;
    public static Settings CurrentSettings
    {
        get
        {
            if(PlatformDetector.IsAndroid())
            {
                if (_settings == null)
                {
                    _settings = ParseFromAppSettings();

                    _settings.Validate();
                }
                return _settings;
            }
            else
            {
                if (_settings == null)
                {
                    _settings = ParseFromXml();

                    _settings.Validate();
                }
                return _settings;
            }

        }
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.ImmichServerUrl))
            throw new SettingsNotValidException($"Settings element '{nameof(ImmichServerUrl)}' is required!");

        if (string.IsNullOrWhiteSpace(this.ApiKey))
            throw new SettingsNotValidException($"Settings element '{nameof(ApiKey)}' is required!");
    }

    private static Settings ParseFromXml()
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

            if (!properties.Select(x => x.Name).Contains(element.Name.LocalName))
                throw new SettingsNotValidException($"Element '{element.Name.LocalName}' is unknown");

            var property = properties.First(x => x.Name == element.Name.LocalName);

            var value = element.Value.Trim();

            switch (element.Name.LocalName)
            {
                case "ImmichServerUrl":
                    var url = value.TrimEnd('/');
                    // Match URL or IP
                    if (!Regex.IsMatch(url, @"^(https?:\/\/)?(([a-zA-Z0-9\.\-_]+(\.[a-zA-Z]{2,})+)|(\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b))(\:\d{1,5})?$"))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid. (' {value} ')");

                    property.SetValue(settings, url);
                    break;
                case "ApiKey":
                    property.SetValue(settings, value);
                    break;
                case "Albums":
                case "People":
                    var list = new List<Guid>();
                    foreach (var item in element.DescendantNodes().OfType<XElement>().ToList())
                    {
                        if (!Guid.TryParse(item.Value, out var id))
                            throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid. Element '{item.Name.LocalName}' with value '{item.Value}'");

                        list.Add(id);
                    }
                    property.SetValue(settings, list);
                    break;
                case "TransitionDuration":
                    if (!double.TryParse(value, out var doubleDuration))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid. ('{value}')");
                    property.SetValue(settings, TimeSpan.FromSeconds(doubleDuration));                    
                    break;
                case "Interval":
                case "RenewImagesDuration":
                case "ClockFontSize":
                case "PhotoDateFontSize":
                case "ImageDescFontSize":
                case "WeatherFontSize":
                    if (!int.TryParse(value, out var intValue))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid. ('{value}')");
                    property.SetValue(settings, intValue);
                    break;
                case "DownloadImages":
                case "ShowMemories":
                case "ShowClock":
                case "ShowPhotoDate":
                case "ShowImageDesc":
                case "ShowWeather":
                    if (!bool.TryParse(value, out var boolValue))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid. ('{value}')");
                    property.SetValue(settings, boolValue);
                    break;
                case "ClockFormat":
                    property.SetValue(settings, value);
                    break;
                case "PhotoDateFormat":
                    property.SetValue(settings, value);
                    break;
                case "WeatherUnits":
                    if (!Regex.IsMatch(value, @"^(?i)(celsius|fahrenheit)$"))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid. ('{value}')");
                    property.SetValue(settings, value);
                    break;
                case "WeatherLatLong":
                    // Regex match Lat/Lon
                    if (!Regex.IsMatch(value, @"^(-?\d+(\.\d+)?),\s*(-?\d+(\.\d+)?)$"))
                        throw new SettingsNotValidException($"Value of '{element.Name.LocalName}' is not valid. ('{value}')");
                    property.SetValue(settings, value);
                    break;
                default:
                    throw new SettingsNotValidException($"Element '{element.Name.LocalName}' is unknown. ('{value}')");
            }
        }

        return settings;
    }
	private static Settings ParseFromAppSettings()
    {      
        var settings = new Settings();
        settings.ImmichServerUrl = Properties.Settings.Default.ImmichServerUrl;
        settings.ApiKey = Properties.Settings.Default.ApiKey;
        var albumList = new List<Guid>();
        //foreach (var item in Properties.Settings.Default.Albums)
        //{
        //    if (!Guid.TryParse(item, out var id))
        //        throw new SettingsNotValidException($"Value of 'Albums' is not valid. Element '{item}'");

        //    albumList.Add(id);
        //}
        //settings.Albums = albumList;
        //var peopleList = new List<Guid>();
        //foreach (var item in Properties.Settings.Default.People)
        //{
        //    if (!Guid.TryParse(item, out var id))
        //        throw new SettingsNotValidException($"Value of 'People' is not valid. Element '{item}'");

        //    peopleList.Add(id);
        //}
        //settings.People = peopleList;
        settings.Interval = Properties.Settings.Default.Interval;
        settings.TransitionDuration = TimeSpan.FromSeconds(Properties.Settings.Default.TransitionDuration);
        settings.RenewImagesDuration = Properties.Settings.Default.RenewImagesDuration;
        settings.ClockFontSize = Properties.Settings.Default.ClockFontSize;
        settings.PhotoDateFontSize = Properties.Settings.Default.PhotoDateFontSize;
        settings.ImageDescFontSize = Properties.Settings.Default.ImageDescFontSize;
        settings.WeatherFontSize = Properties.Settings.Default.WeatherFontSize;
        settings.DownloadImages = Properties.Settings.Default.DownloadImages;
        settings.ShowMemories = Properties.Settings.Default.ShowMemories;
        settings.ShowClock = Properties.Settings.Default.ShowClock;
        settings.ShowPhotoDate = Properties.Settings.Default.ShowPhotoDate;
        settings.ShowImageDesc = Properties.Settings.Default.ShowImageDesc;
        settings.ShowWeather = Properties.Settings.Default.ShowWeather;
        settings.ClockFormat = Properties.Settings.Default.ClockFormat;
        settings.PhotoDateFormat = Properties.Settings.Default.PhotoDateFormat;
        settings.WeatherUnits = Properties.Settings.Default.WeatherUnits;
        settings.WeatherLatLong = Properties.Settings.Default.WeatherLatLong;
        return settings;
    }
}

