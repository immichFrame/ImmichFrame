using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ImmichFrame.Models;
using System.IO;
using Avalonia.Media;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using ThumbHashes;
using System;
using System.Text.RegularExpressions;
using ImmichFrame.Helpers;

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

    public void SetImage(Bitmap image)
    {
        Image = image;
    }
    public async Task SetImage(AssetResponseDto asset)
    {
        var hash = Convert.FromBase64String(asset.Thumbhash);
        var thumbhash = new ThumbHash(hash);

        using (Stream tmbStream = ImageHelper.SaveDataUrlToStream(thumbhash.ToDataUrl()))
        using (Stream imgStream = await asset.AssetImage)
        {
            Image = new Bitmap(imgStream);

            ThumbhashImage = new Bitmap(tmbStream);
            ImageDate = asset?.FileCreatedAt.ToString(_settings.PhotoDateFormat) ?? string.Empty;
            ImageDesc = asset?.ImageDesc ?? string.Empty;
        }
    }

    private Bitmap _backgroundColor;
    public Bitmap ThumbhashImage
    {
        get { return _backgroundColor; }
        private set
        {
            _backgroundColor = value;
            OnPropertyChanged(nameof(ThumbhashImage));
        }
    }

    public Bitmap? Image
    {
        get { return _image; }
        private set
        {
            _image = value;
            OnPropertyChanged(nameof(Image));
        }
    }
    public string ImageDate
    {
        get { return _imageDate; }
        private set
        {
            _imageDate = value;
            OnPropertyChanged(nameof(ImageDate));
        }
    }
    public string ImageDesc
    {
        get { return _imageDesc; }
        private set
        {
            _imageDesc = value;
            OnPropertyChanged(nameof(ImageDesc));
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
