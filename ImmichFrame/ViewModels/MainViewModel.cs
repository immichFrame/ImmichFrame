using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ImmichFrame.Models;

namespace ImmichFrame.ViewModels;

public partial class MainViewModel : INotifyPropertyChanged
{
    private Bitmap? _image;
    private string _imageDate = "";
    private string _liveTime = "";
    private bool _showClock;
    private int _clockFontSize;
    private bool _showPhotoDate;
    private int _photoDateFontSizeSize;
    private bool _showWeather;
    private int _weatherFontSize;
    private string _weatherCurrent = "";
    private string _weatherTemperature = "";
    public Bitmap? Image
    {
        get { return _image; }
        set
        {
            _image = value;
            OnPropertyChanged(nameof(Image));
        }
    }
    public string ImageDate
    {
        get { return _imageDate; }
        set
        {
            _imageDate = value;
            OnPropertyChanged(nameof(ImageDate));
        }
    }
    public string LiveTime
    {
        get { return _liveTime; }
        set
        {
            _liveTime = value;
            OnPropertyChanged(nameof(LiveTime));
        }
    }
    public bool ShowClock
    {
        get { return _showClock; }
        set
        {
            _showClock = value;
            OnPropertyChanged(nameof(ShowClock));
        }
    }
    public int ClockFontSize
    {
        get { return _clockFontSize; }
        set
        {
            _clockFontSize = value;
            OnPropertyChanged(nameof(ClockFontSize));
        }
    }
    public bool ShowPhotoDate
    {
        get { return _showPhotoDate; }
        set
        {
            _showPhotoDate = value;
            OnPropertyChanged(nameof(ShowPhotoDate));
        }
    }
    public int PhotoDateFontSize
    {
        get { return _photoDateFontSizeSize; }
        set
        {
            _photoDateFontSizeSize = value;
            OnPropertyChanged(nameof(PhotoDateFontSize));
        }
    }
    public bool ShowWeather
    {
        get { return _showWeather; }
        set
        {
            _showWeather = value;
            OnPropertyChanged(nameof(ShowWeather));
        }
    }
    public int WeatherFontSize
    {
        get { return _weatherFontSize; }
        set
        {
            _weatherFontSize = value;
            OnPropertyChanged(nameof(WeatherFontSize));
        }
    }
    public string WeatherCurrent
    {
        get { return _weatherCurrent; }
        set
        {
            _weatherCurrent = value;
            OnPropertyChanged(nameof(WeatherCurrent));
        }
    }
    public string WeatherTemperature
    {
        get { return _weatherTemperature; }
        set
        {
            _weatherTemperature = value;
            OnPropertyChanged(nameof(WeatherTemperature));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainViewModel()
    {
        var settings = Settings.CurrentSettings;

        this.ShowClock = settings.ShowClock;
        this.ClockFontSize = settings.ClockFontSize;
        this.ShowPhotoDate = settings.ShowPhotoDate;
        this.PhotoDateFontSize = settings.PhotoDateFontSize;
        this.ShowWeather = settings.ShowWeather;
        this.WeatherFontSize = settings.WeatherFontSize;
    }
}
