using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using System;
using System.Globalization;
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
    private PreloadedAsset? NextAsset;
    private AssetHelper _assetHelper;
    private CultureInfo culture;

    public ICommand NextImageCommand { get; set; }
    public ICommand PreviousImageCommand { get; set; }
    public ICommand PauseImageCommand { get; set; }
    public ICommand NavigateSettingsPageCommand { get; set; }
    public MainViewModel()
    {
        settings = Settings.CurrentSettings;
        _assetHelper = new AssetHelper();
        culture = new CultureInfo(settings.Language);
        NextImageCommand = new RelayCommand(NextImageAction);
        PreviousImageCommand = new RelayCommand(PreviousImageAction);
        PauseImageCommand = new RelayCommand(PauseImageAction);
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

            ImageDate = asset?.LocalDateTime.ToString(Settings.PhotoDateFormat, culture) ?? string.Empty;
            ImageDesc = asset?.ImageDesc ?? string.Empty;

            if (asset?.ExifInfo != null)
            {
                var locationData = new[] {
                    asset.ExifInfo.City,
                    asset.ExifInfo.Country
                }.Where(x => !string.IsNullOrWhiteSpace(x));

                ImageLocation = string.Join(", ", locationData);
            }
            else
            {
                ImageLocation = string.Empty;
            }
        }
        if (Settings.UseImmichFrameAlbum)
        {
            await _assetHelper.AddAssetToAlbum(asset!);
        }

    }

    public async void NextImageTick(object? state)
    {
        await ShowNextImage();
    }
    public void LiveTimeTick(object? state)
    {
        LiveTime = DateTime.Now.ToString(Settings.ClockFormat, culture);
    }
    public void WeatherTick(object? state)
    {
        var weatherInfo = WeatherHelper.GetWeather().Result;
        if (weatherInfo != null)
        {
            WeatherTemperature = $"{weatherInfo.Main.Temperature:F1}{Environment.NewLine}{weatherInfo.CityName}";
            WeatherCurrent = $"{string.Join(',', weatherInfo.Weather.Select(x => x.Description))}";
        }
    }

    public void NavigateSettingsPageAction()
    {
        Navigate(new SettingsViewModel());
    }

    public async void NextImageAction()
    {
        ResetTimer?.Invoke(this, new EventArgs());
        // Needs to run on another thread, android does not allow running network stuff on the main thread
        await Task.Run(ShowNextImage);
    }

    public async Task ShowNextImage()
    {
        int attempt = 0;

        while (attempt < 3)
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

                break;
            }
            catch (AssetNotFoundException)
            {
                // Do not show message and break the loop
                break;
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= 3)
                {
                    this.Navigate(new ErrorViewModel(ex));
                }
            }
        }
    }

    public async void PreviousImageAction()
    {
        if (!ImagePaused)
        {
            ResetTimer?.Invoke(this, new EventArgs());
            // Needs to run on another thread, android does not allow running network stuff on the main thread
            await Task.Run(ShowPreviousImage);
        }
    }
    public async Task ShowPreviousImage()
    {
        int attempt = 0;

        while (attempt < 3)
        {
            try
            {
                if (LastAsset != null)
                {
                    TimerEnabled = false;
                    await SetImage(LastAsset);
                    TimerEnabled = true;
                }

                break;
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= 3)
                {
                    this.Navigate(new ErrorViewModel(ex));
                }
            }
        }
    }
    public void PauseImageAction()
    {
        PauseImage();
    }
    public void PauseImage()
    {
        ImagePaused = !ImagePaused;
        TimerEnabled = !ImagePaused;
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
    private string? imageLocation;
    [ObservableProperty]
    private string? liveTime;
    [ObservableProperty]
    private string? weatherCurrent;
    [ObservableProperty]
    private string? weatherTemperature;
    [ObservableProperty]
    private bool imagePaused = false;
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