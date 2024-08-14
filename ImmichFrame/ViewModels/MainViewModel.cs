using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

    public bool TimerEnabled = false;
    private AssetResponseDto? LastAsset;
    private AssetResponseDto? CurrentAsset;
    private PreloadedAsset? NextAsset;
    private AssetHelper _assetHelper;
    private CultureInfo culture;
    System.Threading.Timer? timerImageSwitcher;
    System.Threading.Timer? timerLiveTime;
    System.Threading.Timer? timerWeather;

    public ICommand NextImageCommand { get; set; }
    public ICommand PreviousImageCommand { get; set; }
    public ICommand PauseImageCommand { get; set; }
    public ICommand QuitCommand { get; set; }
    public ICommand NavigateSettingsPageCommand { get; set; }
    public MainViewModel()
    {
        settings = Settings.CurrentSettings;
        _assetHelper = new AssetHelper();
        culture = new CultureInfo(settings.Language);
        NextImageCommand = new RelayCommand(async () => await NextImageAction());
        PreviousImageCommand = new RelayCommand(async () => await PreviousImageAction());
        PauseImageCommand = new RelayCommand(PauseImageAction);
        QuitCommand = new RelayCommand(ExitApp);
        NavigateSettingsPageCommand = new RelayCommand(NavigateSettingsPageAction);
    }

    public async Task InitializeAsync()
    {
        ShowSplash();
        var settings = Settings;

        if (settings == null)
            throw new SettingsNotValidException("Settings could not be parsed.");

        // Perform async initialization tasks
        await Task.Run(() => _assetHelper.DeleteAndCreateImmichFrameAlbum());
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
        if (asset.ThumbhashImage == null)
            return;

        using (Stream tmbStream = asset.ThumbhashImage)
        using (Stream imgStream = preloadedAsset ?? await asset.AssetImage)
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
            WeatherImage = await ImageHelper.LoadImageFromWeb(new Uri($"https://openweathermap.org/img/wn/{iconId}.png"));
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

    public async Task Preload()
    {
        _image = await Asset.AssetImage;
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