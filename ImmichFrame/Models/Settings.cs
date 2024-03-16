using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ImmichFrame.Models;


public class Settings
{
    public string ImmichServerUrl { get; set; }
    public string ApiKey { get; set; }
    public int Interval { get; set; }
    public bool DownloadImages { get; set; }
    public bool OnlyMemories { get; set; }
    public int RenewImagesDuration { get; set; }
    public List<Guid> Albums { get; set; }
    public bool ShowClock { get; set; }
    public int ClockFontSize { get; set; }
    public string? ClockFormat { get; set; }
    public bool ShowPhotoDate { get; set; }
    public int PhotoDateFontSize { get; set; }
    public string? PhotoDateFormat { get; set; }
    public bool ShowImageDesc { get; set; }
    public int ImageDescFontSize { get; set; }
    public bool ShowWeather { get; set; }
    public int WeatherFontSize { get; set; }
    public string? WeatherUnits { get; set; }
    public string? WeatherLatLong { get; set; }

    private static Settings _settings;
    public static Settings CurrentSettings
    {
        get
        {
            if (_settings == null)
            {
                _settings = Parse();
            }
            return _settings;
        }
    }

    private static Settings Parse()
    {
        var xml = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"Settings.xml");

        var doc = XDocument.Parse(xml).Root!;

        var settings = new Settings
        {
            ImmichServerUrl = doc.Element("ImmichServerUrl")!.Value,
            ApiKey = doc.Element("ApiKey")?.Value ?? string.Empty,
            Albums = doc.Element("Albums")?.DescendantNodes().OfType<XElement>().Select(x => Guid.Parse(x.Value)).Distinct().ToList() ?? new(),
            Interval = int.Parse(doc.Element("Interval")!.Value),
            DownloadImages = Convert.ToBoolean(doc.Element("DownloadImages")?.Value),
            OnlyMemories = Convert.ToBoolean(doc.Element("OnlyMemories")?.Value),
            RenewImagesDuration = int.Parse(doc.Element("RenewImagesDuration")!.Value),
            ShowClock = Convert.ToBoolean(doc.Element("ShowClock")?.Value),
            ClockFontSize = int.Parse(doc.Element("ClockFontSize")!.Value),
            ClockFormat = doc.Element("ClockFormat")!.Value,
            ShowPhotoDate = Convert.ToBoolean(doc.Element("ShowPhotoDate")?.Value),
            PhotoDateFontSize = int.Parse(doc.Element("PhotoDateFontSize")!.Value),
            PhotoDateFormat = doc.Element("PhotoDateFormat")!.Value,
            ShowImageDesc = Convert.ToBoolean(doc.Element("ShowImageDesc")?.Value),
            ImageDescFontSize = int.Parse(doc.Element("ImageDescFontSize")!.Value),
            ShowWeather = Convert.ToBoolean(doc.Element("ShowWeather")?.Value),
            WeatherFontSize = int.Parse(doc.Element("WeatherFontSize")!.Value),
            WeatherUnits = doc.Element("WeatherUnits")!.Value,
            WeatherLatLong = doc.Element("WeatherLatLong")!.Value,
        };

        return settings;
    }
}

