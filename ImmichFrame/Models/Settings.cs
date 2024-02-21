using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ImmichFrame.Models;


public class Settings
{
    public string? ImmichServerUrl { get; set; }
    public string ApiKey { get; set; }
    public int Interval { get; set; }
    public List<Guid>? Albums { get; set; }
    public bool ShowClock { get; set; }
    public int ClockFontSize { get; set; }
    public string? ClockFormat { get; set; }
    public bool ShowPhotoDate { get; set; }
    public int PhotoDateFontSize { get; set; }
    public string? PhotoDateFormat { get; set; }
    public bool ShowWeather { get; set; }
    public int WeatherFontSize { get; set; }
    public string? WeatherUnits { get; set; }
    public string? WeatherLatLong { get; set; }

    public static Settings Parse()
    {
        var xml = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"Settings.xml");

        var doc = XDocument.Parse(xml).Root;

        var settings = new Settings
        {
            ImmichServerUrl = doc.Element("ImmichServerUrl")!.Value,
            ApiKey = doc.Element("ApiKey")?.Value ?? string.Empty,
            Albums = doc.Element("Albums")?.DescendantNodes().OfType<XElement>().Select(x => Guid.Parse(x.Value)).ToList() ?? new(),
            Interval = int.Parse(doc.Element("Interval")!.Value),
            ShowClock = bool.Parse(doc.Element("ShowClock")!.Value),
            ClockFontSize = int.Parse(doc.Element("ClockFontSize")!.Value),
            ClockFormat = doc.Element("ClockFormat")!.Value,
            ShowPhotoDate = bool.Parse(doc.Element("ShowPhotoDate")!.Value),
            PhotoDateFontSize = int.Parse(doc.Element("PhotoDateFontSize")!.Value),
            PhotoDateFormat = doc.Element("PhotoDateFormat")!.Value,
            ShowWeather = bool.Parse(doc.Element("ShowWeather")!.Value),
            WeatherFontSize = int.Parse(doc.Element("WeatherFontSize")!.Value),
            WeatherUnits = doc.Element("WeatherUnits")!.Value,
            WeatherLatLong = doc.Element("WeatherLatLong")!.Value,
        };

        return settings;
    }
}

