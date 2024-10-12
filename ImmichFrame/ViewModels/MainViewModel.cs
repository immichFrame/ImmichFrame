using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;

namespace ImmichFrame.ViewModels;

public partial class MainViewModel : NavigatableViewModelBase
{

    public bool TimerEnabled = false;
    private AssetResponseDto? LastAsset;
    private AssetResponseDto? CurrentAsset;
    private PreloadedAsset? NextAsset;
    private IImmichFrameLogic _immichLogic;
    private CultureInfo culture;
    System.Threading.Timer? timerImageSwitcher;
    System.Threading.Timer? timerLiveTime;
    System.Threading.Timer? timerWeather;
    private CancellationTokenSource? _zoomCancellationTokenSource;

    public ICommand NextImageCommand { get; set; }
    public ICommand PreviousImageCommand { get; set; }
    public ICommand PauseImageCommand { get; set; }
    public ICommand QuitCommand { get; set; }
    public ICommand NavigateSettingsPageCommand { get; set; }

    private bool isInitialized;
    public MainViewModel()
    {
        settings = Settings.CurrentSettings;
        _immichLogic = new ImmichFrameLogic(Settings.CurrentSettings);
        culture = new CultureInfo(settings.Language);
        NextImageCommand = new RelayCommand(async () => await NextImageAction());
        PreviousImageCommand = new RelayCommand(async () => await PreviousImageAction());
        PauseImageCommand = new RelayCommand(PauseImageAction);
        QuitCommand = new RelayCommand(ExitApp);
        NavigateSettingsPageCommand = new RelayCommand(NavigateSettingsPageAction);
    }

    public async Task InitializeAsync()
    {
        if (!isInitialized)
        {
            ShowSplash();
            var settings = Settings;

            if (settings == null)
                throw new SettingsNotValidException("Settings could not be parsed.");

            // Perform async initialization tasks
            await Task.Run(() => _immichLogic.DeleteAndCreateImmichFrameAlbum());
            await ShowNextImage();

            TimerEnabled = true;
            timerImageSwitcher = new System.Threading.Timer(NextImageTick, null, 0, settings.Interval * 1000);
            if (settings.ShowClock)
            {
                timerLiveTime = new System.Threading.Timer(LiveTimeTick, null, 0, 1 * 1000); //every second
            }
            if (settings.ShowWeather)
            {
                timerWeather = new System.Threading.Timer(WeatherTick, null, 0, 10 * 60 * 1000); //every 10 minutes
            }
            isInitialized = true;
        }
    }
    public void SetImage(Bitmap image)
    {
        Images = new UiImage
        {
            Image = image,
            ImageStretch = StretchHelper.FromString(Settings.ImageStretch),
        };
    }

    public async Task SetImage(PreloadedAsset asset)
    {
        await SetImage(asset.Asset, asset.Image);
    }
    public async Task SetImage(AssetResponseDto asset, Stream? preloadedAsset = null)
    {
        _zoomCancellationTokenSource?.Cancel();
        _zoomCancellationTokenSource = new CancellationTokenSource();
        var token = _zoomCancellationTokenSource.Token;

        var thumbHash = asset.ThumbhashImage;
        if (thumbHash == null)
            return;

        using (Stream tmbStream = thumbHash)
        using (Stream imgStream = preloadedAsset ?? await asset.ServeImage(_immichLogic))
        {
            Images = new UiImage
            {
                Image = new Bitmap(imgStream),
                ThumbhashImage = new Bitmap(tmbStream),
                ImageStretch = StretchHelper.FromString(Settings.ImageStretch),
            };

            ImageDate = asset?.LocalDateTime.ToString(Settings.PhotoDateFormat, culture) ?? string.Empty;
            ImageDesc = asset?.ImageDesc ?? string.Empty;

            if (asset?.ExifInfo != null)
            {
                ImageLocation = LocationHelper.GetLocationString(asset.ExifInfo);
            }
            else
            {
                ImageLocation = string.Empty;
            }
        }
        if (Settings.UseImmichFrameAlbum)
        {
            await _immichLogic.AddAssetToAlbum(asset!);
        }
        if (Settings.ImageZoom)
        {
            await ZoomImage(token);
        }
    }
    private async Task ZoomImage(CancellationToken token)
    {
        double targetScale = ImageScale > 1.0 ? 1.0 : 1.25;
        double increment = ImageScale > 1.0 ? -0.0005 : 0.0005;

        for (double scale = ImageScale;
             (increment > 0 && scale < targetScale) || (increment < 0 && scale >= targetScale);
             scale += increment)
        {
            if (token.IsCancellationRequested)
            {
                ImageScale = 1.0;
                break;
            }

            ImageScale = scale;
            await Task.Delay(10);
        }
    }


    private void ShowSplash()
    {
        var uri = new Uri("avares://ImmichFrame/Assets/Immich.png");
        var bitmap = new Bitmap(AssetLoader.Open(uri));
        SetImage(bitmap);
    }

    public async void NextImageTick(object? state)
    {
        await ShowNextImage();
    }
    public void LiveTimeTick(object? state)
    {
        LiveTime = DateTime.Now.ToString(Settings.ClockFormat, culture);
    }
    public async void WeatherTick(object? state)
    {
        var weatherInfo = await WeatherHelper.GetWeather();
        if (weatherInfo != null)
        {
            WeatherTemperature = $"{weatherInfo.Main.Temperature.ToString("F0").Replace(" ", "")}";
            WeatherCurrent = $"{string.Join(',', weatherInfo.Weather.Select(x => x.Description))}";
            var iconId = $"{string.Join(',', weatherInfo.Weather.Select(x => x.IconId))}";
            WeatherImage = new Bitmap(AssetLoader.Open(AssetLoader.Exists(new Uri($"avares://ImmichFrame/Assets/WeatherIcons/{iconId}.png"))
           ? new Uri($"avares://ImmichFrame/Assets/WeatherIcons/{iconId}.png")
           : new Uri("avares://ImmichFrame/Assets/WeatherIcons/default.png")));
        }
    }
    public void NavigateSettingsPageAction()
    {
        Navigate(new SettingsViewModel());
    }

    public async Task NextImageAction()
    {
        ResetTimer();
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
                        CurrentAsset = await _immichLogic.GetNextAsset();

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
                        var asset = await _immichLogic.GetNextAsset();
                        if (asset != null)
                        {
                            NextAsset = new PreloadedAsset(asset);
                            // Preload the actual Image
                            await NextAsset.Preload(_immichLogic);
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
                    if (Settings.UnattendedMode)
                    {
                        // Do not show message and break the loop
                        break;
                    }
                    this.Navigate(new ErrorViewModel(ex));
                }
            }
        }
    }

    public async Task PreviousImageAction()
    {
        if (!ImagePaused)
        {
            ResetTimer();
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
    public void ResetTimer()
    {
        timerImageSwitcher?.Change(Settings.Interval * 1000, Settings.Interval * 1000);
    }

    public void ExitApp()
    {
        timerImageSwitcher?.Dispose();
        timerLiveTime?.Dispose();
        timerWeather?.Dispose();
        Environment.Exit(0);
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
    private double imageScale = 1.0;
    [ObservableProperty]
    private string? liveTime;
    [ObservableProperty]
    private string? weatherCurrent;
    [ObservableProperty]
    private string? weatherTemperature;
    [ObservableProperty]
    private Bitmap? weatherImage;
    [ObservableProperty]
    private bool imagePaused = false;
}

public class PreloadedAsset
{
    public AssetResponseDto Asset { get; }
    private Stream? _image;
    public Stream? Image => _image;
    public PreloadedAsset(AssetResponseDto asset)
    {
        Asset = asset;
    }

    public async Task Preload(IImmichFrameLogic logic)
    {
        _image = await Asset.ServeImage(logic);
    }
}
public static class StretchHelper
{
    public static Stretch FromString(string stretch)
    {
        return Enum.TryParse(stretch, out Stretch result) ? result : Stretch.Uniform;
    }

    public static string ToString(Stretch stretch)
    {
        return stretch.ToString();
    }
}