using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
