using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ImmichFrame.ViewModels;

public partial class MainViewModel : NavigatableViewModelBase
{
    public event EventHandler? ResetTimer;

    public bool TimerEnabled = false;
    private AssetResponseDto? LastAsset;
    private AssetResponseDto? CurrentAsset;
    private AssetHelper _assetHelper;

    public ICommand NextImageCommand { get; set; }
    public ICommand PreviousImageCommand { get; set; }
    public ICommand NavigateSettingsPageCommand { get; set; }
    public MainViewModel()
    {
        settings = Settings.CurrentSettings;
        _assetHelper = new AssetHelper();

        NextImageCommand = new RelayCommand(NextImageAction);
        PreviousImageCommand = new RelayCommand(PreviousImageAction);
        NavigateSettingsPageCommand = new RelayCommand(NavigateSettingsPageAction);
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

            ImageDate = asset?.FileCreatedAt.ToString(Settings.PhotoDateFormat) ?? string.Empty;
            ImageDesc = asset?.ImageDesc ?? string.Empty;
        }
    }

    public async void NextImageTick(object? state)
    {
        await ShowNextImage();
    }
    public void LiveTimeTick(object? state)
    {
        LiveTime = DateTime.Now.ToString(Settings.ClockFormat);
    }
    public void WeatherTick(object? state)
    {
        var weatherInfo = WeatherHelper.GetWeather().Result;
        if (weatherInfo != null)
        {
            WeatherTemperature = $"{weatherInfo.Main.Temperature}{Environment.NewLine}{weatherInfo.CityName}";
            WeatherCurrent = $"{string.Join(',', weatherInfo.Weather.Select(x => x.Description))}";
        }
    }

    public void NavigateSettingsPageAction()
    {
        if (!Settings.IsFromXmlFile || true)
        {
            Navigate(new SettingsViewModel());
        }
    }

    public async void NextImageAction()
    {
        ResetTimer?.Invoke(this, new EventArgs());
        await ShowNextImage();
    }

    public async Task ShowNextImage()
    {
        try
        {
            if (TimerEnabled)
            {
                LastAsset = CurrentAsset;
                CurrentAsset = await _assetHelper.GetNextAsset();
                if (CurrentAsset != null)
                {
                    await SetImage(CurrentAsset);
                }
            }
        }
        catch (AssetNotFoundException)
        {
            // Do not show message
        }
        catch (Exception ex)
        {
            TimerEnabled = false;
            await ShowMessageBoxFromThread(ex.Message, "");
            TimerEnabled = true;
        }
    }

    public async void PreviousImageAction()
    {
        ResetTimer?.Invoke(this, new EventArgs());
        await ShowPreviousImage();
    }
    public async Task ShowPreviousImage()
    {
        try
        {
            if (LastAsset != null)
            {
                TimerEnabled = false;
                await SetImage(LastAsset);
                TimerEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBoxFromThread(ex.Message, "");
        }
    }


    [ObservableProperty]
    public Settings settings;
    [ObservableProperty]
    private UiImage? images;
    [ObservableProperty]
    private string? imageDate;
    [ObservableProperty]
    private string? imageDesc;
    [ObservableProperty]
    private string? liveTime;
    [ObservableProperty]
    private string? weatherCurrent;
    [ObservableProperty]
    private string? weatherTemperature;
}
