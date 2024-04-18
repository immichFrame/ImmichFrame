using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ImmichFrame.ViewModels;

public partial class MainViewModel : NavigatableViewModelBase
{
    public event EventHandler? ResetTimer;

    public bool TimerEnabled = false;
    private AssetResponseDto? LastAsset;
    private AssetResponseDto? CurrentAsset;
    private PreloadedAsset? NextAsset;
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

    public async Task SetImage(PreloadedAsset asset)
    {
        await SetImage(asset.Asset, asset.Image);
    }

    public async Task SetImage(AssetResponseDto asset, Stream? preloadedAsset = null)
    {
        if (asset.ThumbhashImage == null)
            return;

        using (Stream tmbStream = asset.ThumbhashImage)
        using (Stream imgStream = preloadedAsset ?? await asset.AssetImage)
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

        // Needs to run on another task, android does not allow running network stuff on the main thread
        await Task.Run(ShowNextImage);
    }

    public async Task ShowNextImage()
    {
        try
        {
            if (TimerEnabled)
            {
                LastAsset = CurrentAsset;

                if (NextAsset?.Image == null)
                {
                    // Load Image if next image was not ready
                    CurrentAsset = await _assetHelper.GetNextAsset();

                    if (CurrentAsset != null)
                    {
                        await SetImage(CurrentAsset);
                    }
                }
                else
                {
                    // Use preloaded asset

                    await SetImage(NextAsset);
                    CurrentAsset = NextAsset.Asset;
                    NextAsset = null;
                }

                // Load next asset without waiting
                _ = Task.Run(async () =>
                {
                    var asset = await _assetHelper.GetNextAsset();
                    if (asset != null)
                    {
                        NextAsset = new PreloadedAsset(asset);
                        // Preload the actual Image
                        await NextAsset.Preload();
                    }
                });
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

public class PreloadedAsset
{
    public AssetResponseDto Asset { get; }
    private Stream? _image;
    public Stream? Image
    {
        get
        {
            return _image;
        }
    }
    public PreloadedAsset(AssetResponseDto asset)
    {
        Asset = asset;
    }

    public async Task Preload()
    {
        _image = await Asset.AssetImage;
    }
}