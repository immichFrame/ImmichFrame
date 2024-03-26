using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ImmichFrame.Models;

namespace ImmichFrame.ViewModels;

public partial class MainViewModel : INotifyPropertyChanged
{
    private Bitmap? _image;
    private string _imageDate = "";
    private string _imageDesc = "";
    private string _liveTime = "";
    private string _weatherCurrent = "";
    private string _weatherTemperature = "";
    public Settings _settings;
    public Settings Settings
    {
        get { return _settings; }
        set
        {
            _settings = value;
            OnPropertyChanged(nameof(Settings));
        }
    }
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
    public string ImageDesc
    {
        get { return _imageDesc; }
        set
        {
            _imageDesc = value;
            OnPropertyChanged(nameof(ImageDesc));
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
    }
}
