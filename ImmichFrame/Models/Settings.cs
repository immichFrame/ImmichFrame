using Avalonia.Media;
using Avalonia.Platform.Storage;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImmichFrame.Models
{
    public partial class Settings : IFullSettings
    {
        [GeneratedRegex(@"^(([0-9]+)||([0-9]+\,[0-9]+)||([0-9]+\,[0-9]+\,[0-9]+\,[0-9]+))$")]
        private static partial Regex MarginRegex();

        [GeneratedRegex(@"^(?i)(metric|imperial)$")]
        private static partial Regex UnitRegex();

        [GeneratedRegex(@"^(-?[0-9]+(\.[0-9]+)?),\s*(-?[0-9]+(\.[0-9]+)?)$")]
        private static partial Regex LatLongRegex();

        [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}){1,2}$")]
        private static partial Regex HexColorRegex();

        [GeneratedRegex("^(City|City,State|City,State,Country)$")]
        private static partial Regex ImageLocationFormatRegex();


        public string ImmichServerUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ImageStretch { get; set; } = "Uniform";
        public string Margin { get; set; } = "0,0,0,0";
        public int Interval { get; set; } = 45;
        public double TransitionDuration { get; set; } = 1;
        public bool DownloadImages { get; set; } = false;
        public bool ShowMemories { get; set; } = false;
        public int RenewImagesDuration { get; set; } = 20;
        public List<Guid> Albums { get; set; } = new List<Guid>();
        public List<Guid> ExcludedAlbums { get; set; } = new List<Guid>();
        public List<Guid> People { get; set; } = new List<Guid>();
        public List<string> Webcalendars { get; set; } = new List<string>();
        public int RefreshAlbumPeopleInterval { get; set; } = 12;
        [JsonIgnore]
        public bool UseImmichFrameAlbum => !string.IsNullOrWhiteSpace(ImmichFrameAlbumName);
        public string ImmichFrameAlbumName { get; set; } = string.Empty;
        public bool ShowClock { get; set; } = true;
        public int ClockFontSize { get; set; } = 48;
        public string? ClockFormat { get; set; } = "h:mm tt";
        public bool ShowPhotoDate { get; set; } = true;
        public int PhotoDateFontSize { get; set; } = 36;
        public string? PhotoDateFormat { get; set; } = "MM/dd/yyyy";
        public bool ShowImageDesc { get; set; } = true;
        public int ImageDescFontSize { get; set; } = 36;
        public bool ShowImageLocation { get; set; } = true;
        public string? ImageLocationFormat { get; set; } = "City,State,Country";
        public int ImageLocationFontSize { get; set; } = 36;
        public string FontColor { get; set; } = "#FFFFFF";
        public string? WeatherApiKey { get; set; } = string.Empty;
        [JsonIgnore]
        public bool ShowWeather => !string.IsNullOrWhiteSpace(WeatherApiKey);
        public bool ShowWeatherDescription { get; set; } = true;
        public int WeatherFontSize { get; set; } = 36;
        public string? UnitSystem { get; set; } = OpenWeatherMap.UnitSystem.Imperial;
        public string? WeatherLatLong { get; set; } = "40.7128,74.0060";
        [JsonIgnore]
        public float WeatherLat => !string.IsNullOrWhiteSpace(WeatherLatLong) ? float.Parse(WeatherLatLong!.Split(',')[0]) : 0f;
        [JsonIgnore]
        public float WeatherLong => !string.IsNullOrWhiteSpace(WeatherLatLong) ? float.Parse(WeatherLatLong!.Split(',')[1]) : 0f;
        public string Language { get; set; } = "en";
        public bool UnattendedMode { get; set; } = false;
        public bool ImageZoom { get; set; } = false;

        //public static string JsonSettingsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");
        public static string JsonSettingsPath
        {
            get
            {
                string basePath;
                if (PlatformDetector.IsAndroid())
                {
                    basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                }
                else
                {
                    basePath = AppDomain.CurrentDomain.BaseDirectory;
                }
                return Path.Combine(basePath, "Settings.json");
            }
        }

        private static Settings? currentSettings;
        public static Settings CurrentSettings
        {
            get
            {
                if (currentSettings == null)
                {
                    if (!File.Exists(JsonSettingsPath))
                    {
                        return GetDefaultSettingsFile();
                    }
                    currentSettings = ParseFromJson();
                    currentSettings.Validate();
                }
                return currentSettings;
            }
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.ImmichServerUrl))
                throw new SettingsNotValidException($"Settings element '{nameof(ImmichServerUrl)}' is required!");

            if (string.IsNullOrWhiteSpace(this.ApiKey))
                throw new SettingsNotValidException($"Settings element '{nameof(ApiKey)}' is required!");
        }

        public static void ReloadFromJson()
        {
            currentSettings = ParseFromJson();
            currentSettings.Validate();
        }
        private static Settings ParseFromJson()
        {
            var json = File.ReadAllText(Settings.JsonSettingsPath);
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (Exception ex)
            {
                throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
            }

            var settingsValues = new Dictionary<string, object>();

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                object value = property.Value;

                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var element in property.Value.EnumerateArray())
                    {
                        list.Add(element.GetString() ?? string.Empty);
                    }
                    value = list;
                }
                else
                {
                    value = property.Value.ToString();
                }

                settingsValues.Add(property.Name, value);
            }

            return ParseSettings(settingsValues);
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
                        var url = value.ToString()!.TrimEnd('/');
                        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? result) || result == null || (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps))
                        {
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not a valid URL: '{url}'");
                        }
                        property.SetValue(settings, url);
                        break;
                    case "Margin":
                        var margin = value.ToString()!;
                        if (!MarginRegex().IsMatch(margin))
                        {
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. (' {value} ')");
                        }

                        property.SetValue(settings, margin);
                        break;
                    case "ApiKey":
                    case "WeatherApiKey":
                        property.SetValue(settings, value);
                        break;
                    case "ImageStretch":
                        var stretch = new Stretch();
                        if (!Enum.TryParse(value.ToString(), true, out stretch) && Enum.IsDefined(typeof(Stretch), stretch))
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                        property.SetValue(settings, value);
                        break;
                    case "Albums":
                    case "ExcludedAlbums":
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
                    case "ImageLocationFontSize":
                    case "WeatherFontSize":
                    case "RefreshAlbumPeopleInterval":
                        if (!int.TryParse(value.ToString(), out var intValue))
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                        property.SetValue(settings, intValue);
                        break;
                    case "DownloadImages":
                    case "ShowMemories":
                    case "ShowClock":
                    case "ShowPhotoDate":
                    case "ShowImageDesc":
                    case "ShowImageLocation":
                    case "ShowWeatherDescription":
                    case "UnattendedMode":
                    case "ImageZoom":
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
                    case "UnitSystem":
                        if (!UnitRegex().IsMatch(value.ToString()!))
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                        value = value.ToString()!.ToLower() == "metric" ? OpenWeatherMap.UnitSystem.Metric : OpenWeatherMap.UnitSystem.Imperial;
                        property.SetValue(settings, value);
                        break;
                    case "WeatherLatLong":
                        // Regex match Lat/Lon
                        if (!LatLongRegex().IsMatch(value.ToString()!))
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                        property.SetValue(settings, value);
                        break;
                    case "FontColor":
                        // Regex match Hex color
                        if (!HexColorRegex().IsMatch(value.ToString()!))
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                        property.SetValue(settings, value);
                        break;
                    case "Language":
                    case "ImmichFrameAlbumName":
                    case "Webcalendars":
                        property.SetValue(settings, value);
                        break;
                    case "ImageLocationFormat":
                        // Regex match ImageLocationFormat
                        if (!ImageLocationFormatRegex().IsMatch(value.ToString()!))
                            throw new SettingsNotValidException($"Value of '{SettingsValue.Key}' is not valid. ('{value}')");
                        property.SetValue(settings, value);
                        break;

                    default:
                        throw new SettingsNotValidException($"Element '{SettingsValue.Key}' is unknown. ('{value}')");
                }
            }

            return settings;
        }

        public static void SaveSettings(Settings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Settings.JsonSettingsPath, json);
        }
        public static async Task BackupSettings(IStorageFile file)
        {
            var settings = CurrentSettings;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await using var stream = await file.OpenWriteAsync();
            using var streamWriter = new StreamWriter(stream);
            await streamWriter.WriteAsync(json);
        }
        public static async Task RestoreSettings(IStorageFile file)
        {
            await using var stream = await file.OpenReadAsync();
            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            var jsonContents = await streamReader.ReadToEndAsync();
            var settings = JsonSerializer.Deserialize<Settings>(jsonContents, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            currentSettings = settings;
            currentSettings!.Validate();
        }
        private static Settings GetDefaultSettingsFile()
        {
            var defaultSettings = new Settings
            {
                ImmichServerUrl = "",
                ApiKey = "",
                ImageStretch = "Uniform",
                ImageZoom = false,
                Margin = "0,0,0,0",
                Interval = 8,
                TransitionDuration = 2,
                DownloadImages = false,
                ShowMemories = false,
                RenewImagesDuration = 20,
                Albums = new List<Guid>(),
                ExcludedAlbums = new List<Guid>(),
                People = new List<Guid>(),
                RefreshAlbumPeopleInterval = 12,
                ImmichFrameAlbumName = "",
                ShowClock = true,
                ClockFontSize = 48,
                ClockFormat = "h:mm tt",
                ShowPhotoDate = true,
                PhotoDateFontSize = 36,
                PhotoDateFormat = "MM/dd/yyyy",
                ShowImageDesc = true,
                ImageDescFontSize = 36,
                ShowImageLocation = true,
                ImageLocationFormat = "City,State,Country",
                ImageLocationFontSize = 36,
                FontColor = "#FFFFFF",
                WeatherApiKey = "",
                ShowWeatherDescription = true,
                WeatherFontSize = 36,
                UnitSystem = "imperial",
                WeatherLatLong = "40.7128,74.0060",
                Language = "en",
                UnattendedMode = false
            };
            return defaultSettings;
        }
    }
}
