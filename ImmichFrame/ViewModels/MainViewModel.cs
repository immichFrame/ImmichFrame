using Avalonia.Media.Imaging;
using ImmichFrame.Models;
using System.IO;
using System.Threading.Tasks;

namespace ImmichFrame.ViewModels;

public partial class MainViewModel : ViewModelBase
{
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
        Images = new UiImage
        {
            Image = image
        };
    }
    public async Task SetImage(AssetResponseDto asset)
    {
        using (Stream tmbStream = asset.ThumbhashImage)
        using (Stream imgStream = await asset.AssetImage)
        {
            Images = new UiImage
            {
                Image = new Bitmap(imgStream),
                ThumbhashImage = new Bitmap(tmbStream)
            };

            ImageDate = asset?.FileCreatedAt.ToString(_settings.PhotoDateFormat) ?? string.Empty;
            ImageDesc = asset?.ImageDesc ?? string.Empty;
        }
    }

    private UiImage? _images;
    public UiImage? Images
    {
        get { return _images; }
        private set
        {
            _images = value;
            OnPropertyChanged(nameof(Images));
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
}
