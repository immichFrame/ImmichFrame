using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ImmichFrame.Models;


public class Settings
{
    public string ImmichServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Interval { get; set; } = 8;
    public double TransitionDuration { get; set; } = 1;
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
            if (PlatformDetector.IsAndroid())
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

    public void Serialize()
    {
        this.Validate();

        if (PlatformDetector.IsAndroid())
        {
            // settings
            var defaultSettings = Properties.Settings.Default;

            defaultSettings.ImmichServerUrl = this.ImmichServerUrl;
            defaultSettings.ApiKey = this.ApiKey;
            defaultSettings.Interval = this.Interval;
            defaultSettings.DownloadImages = this.DownloadImages;
            defaultSettings.ShowMemories = this.ShowMemories;
            defaultSettings.RenewImagesDuration = this.RenewImagesDuration;
            defaultSettings.ShowClock = this.ShowClock;
            defaultSettings.ClockFontSize = this.ClockFontSize;
            defaultSettings.ClockFormat = this.ClockFormat;
            defaultSettings.ShowPhotoDate = this.ShowPhotoDate;
            defaultSettings.PhotoDateFontSize = this.PhotoDateFontSize;
            defaultSettings.PhotoDateFormat = this.PhotoDateFormat;
            defaultSettings.ShowImageDesc = this.ShowImageDesc;
            defaultSettings.ImageDescFontSize = this.ImageDescFontSize;
            defaultSettings.ShowWeather = this.ShowWeather;
            defaultSettings.WeatherFontSize = this.WeatherFontSize;
            defaultSettings.WeatherUnits = this.WeatherUnits;
            defaultSettings.WeatherLatLong = this.WeatherLatLong;

            var albums = new StringCollection();
            if(this.Albums?.Any() ?? false)
                albums.AddRange(this.Albums.Select(x => x.ToString()).ToArray());
            defaultSettings.Albums = albums;

            var people = new StringCollection();
            if (this.People?.Any() ?? false)
                people.AddRange(this.People.Select(x => x.ToString()).ToArray());
            defaultSettings.People = people;
            defaultSettings.TransitionDuration = this.TransitionDuration;

            defaultSettings.Save();
        }
        else
        {
            // xml
            XmlSerializer xsSubmit = new XmlSerializer(typeof(Settings));
            var xml = "";

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, this);
                    xml = sww.ToString();
                }
            }

            // TODO: some error handling with try catch?
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"Settings.xml", xml);
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

        var properties = typeof(Settings).GetProperties();

        var SettingsValues = new Dictionary<string, object>();
        foreach (var element in doc.Elements())
        {
            if (!properties.Select(x => x.Name).Contains(element.Name.LocalName))
                throw new SettingsNotValidException($"Element '{element.Name.LocalName}' is unknown");

            object value = element.Value;

            if (element.HasElements)
                value = element.DescendantNodes().OfType<XElement>().Select(x => x.Value).ToList();

            if (SettingsValues.ContainsKey(element.Name.LocalName))
            {
                SettingsValues[element.Name.LocalName] = value;
            }
            else
            {
                SettingsValues.Add(element.Name.LocalName, value);
            }
        }

        return ParseSettings(SettingsValues);
    }

    private static Settings ParseFromAppSettings()
    {
        var SettingsValues = new Dictionary<string, object>();
        var settings = Properties.Settings.Default;
        foreach (SettingsProperty property in Properties.Settings.Default.Properties)
        {
            var name = property.Name;
            var value = settings[name];

            if (property.PropertyType.Name.ToUpper() == "StringCollection".ToUpper())
            {
                value = (settings[name] as StringCollection)?.Cast<string>().ToList() ?? new List<string>();
            }

            if (SettingsValues.ContainsKey(name))
            {
                SettingsValues[name] = value;
            }
            else
            {
                SettingsValues.Add(name, value);
            }
        }

        return ParseSettings(SettingsValues);
    }

    private static Settings ParseSettings(Dictionary<string, object> SettingsValues)
    {
        var settings = new Settings();
        var properties = settings.GetType().GetProperties();

        foreach (var SettingsValue in SettingsValues)
        {
            var property = properties.First(x => x.Name == SettingsValue.Key);

            var value = SettingsValue.Value;

            if (value == null)
                throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid.");

            switch (SettingsValue.Key)
            {
                case "ImmichServerUrl":
                    var url = value.ToString().TrimEnd('/');
                    // Match URL or IP
                    if (!Regex.IsMatch(url, @"^(https?:\/\/)?(([a-zA-Z0-9\.\-_]+(\.[a-zA-Z]{2,})+)|(\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b))(\:\d{1,5})?$"))
                        throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. (' {value} ')");

                    property.SetValue(settings, url);
                    break;
                case "ApiKey":
                    property.SetValue(settings, value);
                    break;
                case "Albums":
                case "People":
                    var list = new List<Guid>();
                    foreach (var item in (List<string>)(SettingsValue.Value ?? new()))
                    {
                        if (!Guid.TryParse(item, out var id))
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. Element with value '{item}'");

                        list.Add(id);
                    }
                    property.SetValue(settings, list);
                    break;
                case "TransitionDuration":
                    if (!double.TryParse(value.ToString(), out var doubleDuration))
                        throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                    property.SetValue(settings, doubleDuration);
                    break;
                case "Interval":
                case "RenewImagesDuration":
                case "ClockFontSize":
                case "PhotoDateFontSize":
                case "ImageDescFontSize":
                case "WeatherFontSize":
                    if (!int.TryParse(value.ToString(), out var intValue))
                        throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                    property.SetValue(settings, intValue);
                    break;
                case "DownloadImages":
                case "ShowMemories":
                case "ShowClock":
                case "ShowPhotoDate":
                case "ShowImageDesc":
                case "ShowWeather":
                    if (!bool.TryParse(value.ToString(), out var boolValue))
                        throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                    property.SetValue(settings, boolValue);
                    break;
                case "ClockFormat":
                    property.SetValue(settings, value);
                    break;
                case "PhotoDateFormat":
                    property.SetValue(settings, value);
                    break;
                case "WeatherUnits":
                    if (!Regex.IsMatch(value.ToString(), @"^(?i)(celsius|fahrenheit)$"))
                        throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                    property.SetValue(settings, value);
                    break;
                case "WeatherLatLong":
                    // Regex match Lat/Lon
                    if (!Regex.IsMatch(value.ToString(), @"^(-?\d+(\.\d+)?),\s*(-?\d+(\.\d+)?)$"))
                        throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                    property.SetValue(settings, value);
                    break;
                default:
                    throw new SettingsNotValidException($"Element '{SettingsValue.Key}' is unknown. ('{value}')");
            }
        }

        return settings;
    }
}

