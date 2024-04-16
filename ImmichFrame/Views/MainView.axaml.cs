using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using ImmichFrame.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ImmichFrame.Views;

public partial class MainView : BaseView
{
    System.Threading.Timer? timerImageSwitcher;
    System.Threading.Timer? timerLiveTime;
    System.Threading.Timer? timerWeather;
    private bool timerImageSwitcher_Enabled = false;
    private MainViewModel _viewModel;
    private Settings _appSettings;
    private AssetHelper _assetHelper;
    private AssetResponseDto? LastAsset;
    private AssetResponseDto? CurrentAsset;
    public MainView()
    {
        InitializeComponent();
        _appSettings = new Settings();
        _assetHelper = new AssetHelper();
        this.Loaded += OnLoaded;
    }
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (PlatformDetector.IsAndroid())
            {
                var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
                if (insetsManager != null)
                {
                    insetsManager.DisplayEdgeToEdge = true;
                    insetsManager.IsSystemBarVisible = false;
                }
            }
            _viewModel = (this.DataContext as MainViewModel)!;

            _appSettings = _viewModel.Settings;

            if (_appSettings == null)
                throw new SettingsNotValidException("Settings could not be parsed.");

            ShowSplash();

            ShowNextImage();

            if (transitioningControl.PageTransition is CrossFade crossFade)
            {
                crossFade.Duration = TimeSpan.FromSeconds(_appSettings.TransitionDuration);
            }

            timerImageSwitcher_Enabled = true;
            timerImageSwitcher = new System.Threading.Timer(timerImageSwitcherTick, null, 0, _appSettings.Interval * 1000);
            if (_appSettings.ShowClock)
            {
                timerLiveTime = new System.Threading.Timer(timerLiveTimeTick, null, 0, 1000); //every second
            }
            if (_appSettings.ShowWeather)
            {
                timerWeather = new System.Threading.Timer(timerWeatherTick, null, 0, 600000); //every 10 minutes
            }
        }
        catch (SettingsNotValidException ex)
        {
            await ShowMessageBox(ex.Message, "There was a Problem loading the Settings");
            ExitApp();
        }
        catch (Exception ex)
        {
            await ShowMessageBox(ex.Message);
            ExitApp();
        }
    }

    private void timerImageSwitcherTick(object? state)
    {
        ShowNextImage();
    }

    private void timerLiveTimeTick(object? state)
    {
        _viewModel.LiveTime = DateTime.Now.ToString(_appSettings.ClockFormat);
    }
    private void timerWeatherTick(object? state)
    {
        var weatherInfo = WeatherHelper.GetWeather().Result;
        if (weatherInfo != null)
        {
            _viewModel.WeatherTemperature = $"{weatherInfo.Main.Temperature}{Environment.NewLine}{weatherInfo.CityName}";
            _viewModel.WeatherCurrent = $"{string.Join(',', weatherInfo.Weather.Select(x=>x.Description))}";
        }
    }

    private void ShowSplash()
    {
        var uri = new Uri("avares://ImmichFrame/Assets/Immich.png");
        var bitmap = new Bitmap(AssetLoader.Open(uri));
        _viewModel.SetImage(bitmap);
    }

    private async void ShowNextImage()
    {
        try
        {
            if (timerImageSwitcher_Enabled)
            {
                LastAsset = CurrentAsset;
                CurrentAsset = await _assetHelper.GetNextAsset();
                if (CurrentAsset != null)
                {
                    await _viewModel.SetImage(CurrentAsset);
                }
            }
        }
        catch (AssetNotFoundException)
        {
            // Do not show message
        }
        catch (Exception ex)
        {
            timerImageSwitcher_Enabled = false;
            await ShowMessageBoxFromThread(ex.Message);
            timerImageSwitcher_Enabled = true;
        }
    }

    private async void ShowPreviousImage()
    {
        try
        {
            if (LastAsset != null)
            {
                timerImageSwitcher_Enabled = false;
                await _viewModel.SetImage(LastAsset);
                timerImageSwitcher_Enabled = true;
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBoxFromThread(ex.Message);
        }
    }

    public async void btnBack_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(_appSettings.Interval * 1000, _appSettings.Interval * 1000);
        await Task.Run(() => ShowPreviousImage());
    }

    public async void btnNext_Click(object? sender, RoutedEventArgs args)
    {
        timerImageSwitcher!.Change(_appSettings.Interval * 1000, _appSettings.Interval * 1000);
        await Task.Run(() => ShowNextImage());
    }

    public void btnQuit_Click(object? sender, RoutedEventArgs args)
    {
        ExitApp();
    }
    public void btnSettings_Click(object? sender, RoutedEventArgs args)
    {
        if (!Settings.IsFromXmlFile)
        {
            ExitView();
            ((NavigatableViewModelBase)this.DataContext!).Navigate(new SettingsViewModel());
        }
    }

    private void ExitView()
    {
        timerImageSwitcher_Enabled = false;
        timerImageSwitcher?.Dispose();
        timerLiveTime?.Dispose();
        timerWeather?.Dispose();
    }
    private void ExitApp()
    {
        ExitView();
        Environment.Exit(0);
    }
}
