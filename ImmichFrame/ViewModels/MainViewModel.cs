using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Exceptions;
using ImmichFrame.Models;
using System.IO;
using System.Threading.Tasks;

namespace ImmichFrame.ViewModels;

public partial class MainViewModel : NavigatableViewModelBase
{
    public MainViewModel()
    {
        settings = Settings.CurrentSettings;
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
        if (asset.ThumbhashImage == null)
            return;

        using (Stream tmbStream = asset.ThumbhashImage)
        using (Stream imgStream = await asset.AssetImage)
        {
            Images = new UiImage
            {
                Image = new Bitmap(imgStream),
                ThumbhashImage = new Bitmap(tmbStream)
            };

            ImageDate = asset?.FileCreatedAt.ToString(settings.PhotoDateFormat) ?? string.Empty;
            ImageDesc = asset?.ImageDesc ?? string.Empty;
        }
    }

    [ObservableProperty]
    public Settings settings;
    [ObservableProperty]
    private UiImage? images;
    [ObservableProperty]
    private string imageDate;
    [ObservableProperty]
    private string imageDesc;
    [ObservableProperty]
    private string liveTime;
    [ObservableProperty]
    private string weatherCurrent;
    [ObservableProperty]
    private string weatherTemperature;
}
