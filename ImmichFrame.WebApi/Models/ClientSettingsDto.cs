using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Models;

public class ClientSettingsDto
{
    public int Interval { get; set; }
    public double TransitionDuration { get; set; }
    public bool DownloadImages { get; set; }
    public int RenewImagesDuration { get; set; }
    public bool ShowClock { get; set; }
    public string? ClockFormat { get; set; }
    public string? ClockDateFormat { get; set; }
    public bool ShowPhotoDate { get; set; }
    public bool ShowProgressBar { get; set; }
    public string? PhotoDateFormat { get; set; }
    public bool ShowImageDesc { get; set; }
    public bool ShowPeopleDesc { get; set; }
    public bool ShowAlbumName { get; set; }
    public bool ShowImageLocation { get; set; }
    public string? ImageLocationFormat { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string Style { get; set; }
    public string? BaseFontSize { get; set; }
    public bool ShowWeatherDescription { get; set; }
    public bool ShowChronologicalImages { get; set; }
    public int ChronologicalImagesCount { get; set; }
    public string? WeatherIconUrl { get; set; }
    public bool ImageZoom { get; set; }
    public bool ImagePan { get; set; }
    public bool ImageFill { get; set; }
    public string Layout { get; set; }
    public string Language { get; set; }

    public static ClientSettingsDto FromGeneralSettings(IGeneralSettings generalSettings)
    {
        ClientSettingsDto dto = new ClientSettingsDto();
        dto.Interval = generalSettings.Interval;
        dto.TransitionDuration = generalSettings.TransitionDuration;
        dto.DownloadImages = generalSettings.DownloadImages;
        dto.RenewImagesDuration = generalSettings.RenewImagesDuration;
        dto.ShowClock = generalSettings.ShowClock;
        dto.ClockFormat = generalSettings.ClockFormat;
        dto.ClockDateFormat = generalSettings.ClockDateFormat;
        dto.ShowPhotoDate = generalSettings.ShowPhotoDate;
        dto.ShowProgressBar = generalSettings.ShowProgressBar;
        dto.PhotoDateFormat = generalSettings.PhotoDateFormat;
        dto.ShowImageDesc = generalSettings.ShowImageDesc;
        dto.ShowPeopleDesc = generalSettings.ShowPeopleDesc;
        dto.ShowAlbumName = generalSettings.ShowAlbumName;
        dto.ShowImageLocation = generalSettings.ShowImageLocation;
        dto.ImageLocationFormat = generalSettings.ImageLocationFormat;
        dto.PrimaryColor = generalSettings.PrimaryColor;
        dto.SecondaryColor = generalSettings.SecondaryColor;
        dto.Style = generalSettings.Style;
        dto.BaseFontSize = generalSettings.BaseFontSize;
        dto.ShowWeatherDescription = generalSettings.ShowWeatherDescription;
        dto.WeatherIconUrl = generalSettings.WeatherIconUrl;
        dto.ImageZoom = generalSettings.ImageZoom;
        dto.ImagePan = generalSettings.ImagePan;
        dto.ImageFill = generalSettings.ImageFill;
        dto.Layout = generalSettings.Layout;
        dto.ShowChronologicalImages = generalSettings.ShowChronologicalImages;
        dto.ChronologicalImagesCount = generalSettings.ChronologicalImagesCount;
        dto.Language = generalSettings.Language;
        return dto;
    }
}